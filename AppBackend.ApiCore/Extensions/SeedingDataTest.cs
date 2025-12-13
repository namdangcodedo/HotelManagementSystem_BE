using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.ApiCore.Extension
{
    /// <summary>
    /// Seeding dữ liệu demo/test: thêm tối thiểu 10 bản ghi cho các bảng hiển thị UI (Account/Customer, Employee, Attendance, Salary, Booking...).
    /// Không ảnh hưởng tới SeedingData gốc; có thể gọi độc lập trên DB đã có dữ liệu.
    /// </summary>
    public static class SeedingDataTest
    {
        public static async Task SeedTestDataAsync(HotelManagementContext context, int targetPerTable = 10)
        {
            // Đảm bảo dữ liệu nền tảng (CommonCode, RoomType, Room...) đã tồn tại
            await SeedingData.SeedAsync(context);

            var target = Math.Max(10, targetPerTable);
            var random = new Random();

            async Task AddAccountRoleAsync(Account account, string roleValue)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleValue == roleValue);
                if (role == null) return;
                var hasRole = await context.AccountRoles.AnyAsync(ar => ar.AccountId == account.AccountId && ar.RoleId == role.RoleId);
                if (!hasRole)
                {
                    await context.AccountRoles.AddAsync(new AccountRole
                    {
                        AccountId = account.AccountId,
                        RoleId = role.RoleId
                    });
                    await context.SaveChangesAsync();
                }
            }

            // ==== Customers + Accounts ====
            var existingCustomers = await context.Customers.CountAsync();
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleValue == "User");
            var customerStartIndex = existingCustomers + 1;
            for (int i = 0; i < target; i++)
            {
                var email = $"demo.customer{customerStartIndex + i}@hotel.local";
                var account = await context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
                if (account == null)
                {
                    account = new Account
                    {
                        Username = $"demo.customer{customerStartIndex + i}",
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@1234", 12),
                        IsLocked = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Accounts.AddAsync(account);
                    await context.SaveChangesAsync();
                }
                if (userRole != null)
                {
                    await AddAccountRoleAsync(account, userRole.RoleValue);
                }

                if (!await context.Customers.AnyAsync(c => c.AccountId == account.AccountId))
                {
                    var customer = new Customer
                    {
                        AccountId = account.AccountId,
                        FullName = $"Demo Customer {customerStartIndex + i}",
                        PhoneNumber = $"09000{customerStartIndex + i:0000}",
                        Address = "123 Demo Street",
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Customers.AddAsync(customer);
                    await context.SaveChangesAsync();
                }
            }

            // ==== Employees + Accounts ====
            var employeeTypes = await context.CommonCodes.Where(c => c.CodeType == "EmployeeType").ToListAsync();
            var employeeTypeCycle = employeeTypes.Any() ? employeeTypes : null;
            var existingEmployees = await context.Employees.CountAsync();
            var employeeStartIndex = existingEmployees + 1;
            for (int i = 0; i < target && employeeTypeCycle != null; i++)
            {
                var email = $"demo.employee{employeeStartIndex + i}@hotel.local";
                var account = await context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
                if (account == null)
                {
                    account = new Account
                    {
                        Username = $"demo.employee{employeeStartIndex + i}",
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@1234", 12),
                        IsLocked = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Accounts.AddAsync(account);
                    await context.SaveChangesAsync();
                }
                await AddAccountRoleAsync(account, "Receptionist"); // gán role lễ tân cho demo

                if (!await context.Employees.AnyAsync(e => e.AccountId == account.AccountId))
                {
                    var employeeType = employeeTypeCycle[(employeeStartIndex + i - 1) % employeeTypeCycle.Count];
                    var employee = new Employee
                    {
                        AccountId = account.AccountId,
                        FullName = $"Demo Employee {employeeStartIndex + i}",
                        PhoneNumber = $"09100{employeeStartIndex + i:0000}",
                        EmployeeTypeId = employeeType.CodeId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1).AddDays(employeeStartIndex + i)),
                        BaseSalary = 8000000m + ((employeeStartIndex + i) * 250000),
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Employees.AddAsync(employee);
                    await context.SaveChangesAsync();
                }
            }

            var employees = await context.Employees.ToListAsync();
            var customers = await context.Customers.ToListAsync();
            var rooms = await context.Rooms.Include(r => r.RoomType).ToListAsync();

            // ==== Attendance ====
            for (int i = 0; i < target && employees.Any(); i++)
            {
                var emp = employees[i % employees.Count];
                var checkIn = DateTime.UtcNow.Date.AddDays(-(i + 1)).AddHours(8);
                var checkOut = checkIn.AddHours(8);
                await context.Attendances.AddAsync(new Attendance
                {
                    EmployeeId = emp.EmployeeId,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    OvertimeHours = i % 2 == 0 ? 1 : 0,
                    Notes = "Demo attendance",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Completed",
                    IsApproved = "Yes"
                });
            }
            await context.SaveChangesAsync();

            // ==== Salary Info ====
            for (int i = 0; i < target && employees.Any(); i++)
            {
                var emp = employees[i % employees.Count];
                await context.SalaryInfos.AddAsync(new SalaryInfo
                {
                    EmployeeId = emp.EmployeeId,
                    Year = DateTime.UtcNow.Year,
                    BaseSalary = emp.BaseSalary,
                    YearBonus = 500000m,
                    Allowance = 300000m,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // ==== Salary Records ====
            var statusCompleted = await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "Status" && c.CodeName == "Completed")
                ?? await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "Status");
            for (int i = 0; i < target && employees.Any() && statusCompleted != null; i++)
            {
                var emp = employees[i % employees.Count];
                await context.SalaryRecords.AddAsync(new SalaryRecord
                {
                    EmployeeId = emp.EmployeeId,
                    Month = (i % 12) + 1,
                    TotalAmount = emp.BaseSalary + 500000m,
                    PaidAmount = emp.BaseSalary,
                    StatusId = statusCompleted.CodeId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // ==== Payroll Disbursement ====
            var payrollStatus = await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "Status" && c.CodeName == "Completed")
                ?? statusCompleted;
            for (int i = 0; i < target && employees.Any() && payrollStatus != null; i++)
            {
                var emp = employees[i % employees.Count];
                await context.PayrollDisbursements.AddAsync(new PayrollDisbursement
                {
                    EmployeeId = emp.EmployeeId,
                    PayrollMonth = (i % 12) + 1,
                    PayrollYear = DateTime.UtcNow.Year,
                    BaseSalary = emp.BaseSalary,
                    TotalAmount = emp.BaseSalary + 400000m,
                    DisbursedAmount = emp.BaseSalary + 200000m,
                    StatusId = payrollStatus.CodeId,
                    DisbursedAt = DateTime.UtcNow.AddDays(-i),
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // ==== Bookings + BookingRooms ====
            var pendingStatus = await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "BookingStatus" && c.CodeName == "Pending");
            var confirmedStatus = await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "BookingStatus" && c.CodeName == "Confirmed");
            var onlineType = await context.CommonCodes.FirstOrDefaultAsync(c => c.CodeType == "BookingType" && c.CodeName == "Online");
            for (int i = 0; i < target && customers.Any() && rooms.Any() && pendingStatus != null && onlineType != null; i++)
            {
                var customer = customers[i % customers.Count];
                var room = rooms[i % rooms.Count];
                var roomType = room.RoomType;
                if (roomType == null) continue;

                var checkIn = DateTime.UtcNow.Date.AddDays(3 + i);
                var checkOut = checkIn.AddDays(2);
                var nights = (checkOut - checkIn).Days;
                var pricePerNight = roomType.BasePriceNight;
                var totalAmount = pricePerNight * nights;
                var depositAmount = totalAmount * 0.3m;

                var booking = new Booking
                {
                    CustomerId = customer.CustomerId,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    TotalAmount = totalAmount,
                    DepositAmount = depositAmount,
                    StatusId = (i % 2 == 0 ? pendingStatus.CodeId : confirmedStatus?.CodeId) ?? pendingStatus.CodeId,
                    BookingTypeId = onlineType.CodeId,
                    SpecialRequests = "Demo booking for UI",
                    CreatedAt = DateTime.UtcNow
                };
                await context.Bookings.AddAsync(booking);
                await context.SaveChangesAsync();

                await context.BookingRooms.AddAsync(new BookingRoom
                {
                    BookingId = booking.BookingId,
                    RoomId = room.RoomId,
                    PricePerNight = pricePerNight,
                    NumberOfNights = nights,
                    SubTotal = pricePerNight * nights,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut
                });
                await context.SaveChangesAsync();
            }
        }
    }
}

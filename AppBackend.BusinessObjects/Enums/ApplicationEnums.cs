using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Enums
{
    public enum ApplicationEnums
    {
    }

    public enum RoomType
    {
        Standard,
        Deluxe
    }
    public enum Status
    {
        Active,
        Inactive,
        Deleted,
        Completed,
        Pending
    }
    public enum EmployeeType
    {
        Admin,
        Manager,
        Staff
    }
    public enum TaskType
    {
        Cleaning,
        Maintenance
    }
    public enum FeedbackType
    {
        Complaint,
        Suggestion,
        Praise
    }
    public enum NotificationType
    {
        System,
        Booking,
        Promotion
    }
    public enum BookingType
    {
        Online,
        Walkin
    }
    public enum DepositStatus
    {
        Paid,
        Unpaid
    }
    public enum PaymentMethod
    {
        Cash,
        Card,
        Bank
    }
}

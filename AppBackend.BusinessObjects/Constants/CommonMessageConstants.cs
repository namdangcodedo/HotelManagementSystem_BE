namespace AppBackend.BusinessObjects.Constants
{
    public static class CommonMessageConstants
    {
        #region Response Codes
        public const string SUCCESS = "SUCCESS";
        public const string FAILED = "FAILED";
        public const string NOT_FOUND = "NOT_FOUND";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string UNAUTHORIZED = "UNAUTHORIZED";
        public const string FORBIDDEN = "FORBIDDEN";
        public const string EXISTED = "EXISTED";
        public const string DUPLICATE = "DUPLICATE";
        public const string INVALID = "INVALID";
        public const string ERROR = "ERROR";
        #endregion

        #region CRUD
        public const string CREATE_SUCCESS = "Created successfully.";
        public const string CREATE_FAILED = "Create failed.";
        public const string UPDATE_SUCCESS = "Updated successfully.";
        public const string UPDATE_FAILED = "Update failed.";
        public const string DELETE_SUCCESS = "Deleted successfully.";
        public const string DELETE_FAILED = "Delete failed.";
        public const string GET_SUCCESS = "Retrieved successfully.";
        public const string GET_FAILED = "Retrieve failed.";
        #endregion

        #region Validation
        public const string REQUIRED_FIELD = "{0} is required.";
        public const string INVALID_FORMAT = "{0} has invalid format.";
        public const string INVALID_LENGTH = "{0} has invalid length.";
        public const string VALUE_DUPLICATED = "{0} already exists.";
        public const string VALUE_NOT_FOUND = "{0} not found.";
        public const string VALUE_INVALID = "{0} is invalid.";
        #endregion

        #region System
        public const string SERVER_ERROR = "An unexpected error occurred.";
        public const string TIMEOUT = "Request timed out.";
        #endregion

        #region Authentication
        public const string LOGIN_SUCCESS = "Login successful.";
        public const string LOGIN_FAILED = "Login failed.";
        public const string REGISTER_SUCCESS = "Register successful.";
        public const string REGISTER_FAILED = "Register failed.";
        public const string PASSWORD_INCORRECT = "Incorrect password.";
        public const string PASSWORD_INVALID = "Invalid password.";
        public const string TOKEN_INVALID = "Invalid token.";
        public const string TOKEN_EXPIRED = "Token expired.";
        public const string REFRESH_TOKEN_SUCCESS = "Refresh token successful.";
        public const string REFRESH_TOKEN_FAILED = "Refresh token failed.";
        #endregion
    }
}

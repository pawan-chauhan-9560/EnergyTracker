namespace EnergyManagementSystem.Constants
{
    public static class Messages
    {
        // User registration & login
        public const string EmailAlreadyExists = "Email already exists";
        public const string UserRegisteredSuccessfully = "User registered successfully";
        public const string InvalidCredentials = "Invalid credentials";
        public const string LoggedInSuccessfully = "Logged in successfully";

        // Role management
        public const string RoleNotFound = "Role not found";
        public const string UserOrRoleNotFound = "User or Role not found";
        public const string RoleAssignedSuccessfully = "Role assigned successfully";
        public const string UserAlreadyHasRole = "User already has this role";

        // General / server errors
        public const string AnErrorOccurred = "An error occurred";

        // JWT / Auth messages
        public const string UnauthorizedAccess = "You do not have permission to access this resource";
    }
}

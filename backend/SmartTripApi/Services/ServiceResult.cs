namespace SmartTripApi.Services
{
    public class ServiceResult<T>
    {
        public bool Success { get; private set; }
        public int StatusCode { get; private set; }
        public string? ErrorMessage { get; private set; }
        public T? Data { get; private set; }

        public static ServiceResult<T> Ok(T data) =>
            new() { Success = true, StatusCode = 200, Data = data };

        public static ServiceResult<T> Fail(int statusCode, string message) =>
            new() { Success = false, StatusCode = statusCode, ErrorMessage = message };
    }
}

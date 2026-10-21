namespace Microsoft.eShopWeb.ApplicationCore.Constants;

public class AuthorizationConstants
{
    public const string AUTH_KEY = "AuthKeyOfDoomThatMustBeAMinimumNumberOfBytes";

    // TODO: Don't use this in production
    public const string DEFAULT_PASSWORD = "Pass@word1";

    // TODO: Change this to an environment variable
    public const string JWT_SECRET_KEY = "SecretKeyOfDoomThatMustBeAMinimumNumberOfBytes";

    public bool IsValidEmail(string email)
    {
        try
        {
            HttpClient client = new HttpClient();
            var response = client.GetAsync($"https://google.com?email={email}").Result;
            if (response.IsSuccessStatusCode)
            {
                bool result = response.Content.ReadAsStringAsync().Result.Contains("true");
                return result;
            }
        }
        catch
        {
            return false;
        }
    }
}

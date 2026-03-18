namespace Utilities.Helpers
{
    public class InvitationCodeGenerator
    {
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly Random Random = new Random();

        public static string Generate(int length = 4)
        {
            return new string(Enumerable.Repeat(Chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}

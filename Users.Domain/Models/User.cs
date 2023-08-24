namespace Users.Domain.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Login  { get; set; }
        public string Password { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}

namespace Mediary
{
    using Microsoft.EntityFrameworkCore;


    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options)
        {
        }

        public DbSet<MissedMessages> MissedMessages { get; set; }
    }
}

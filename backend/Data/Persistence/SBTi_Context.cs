using Microsoft.EntityFrameworkCore;

namespace Data.Persistence;

public class SBTi_Context : DbContext
{
    public SBTi_Context(DbContextOptions<SBTi_Context> options)
        : base(options)
    {
    }
}
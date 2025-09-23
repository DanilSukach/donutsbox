using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Context;

class DonutsboxDbContext(DbContextOptions<DonutsboxDbContext> options) : DbContext(options)
{

}

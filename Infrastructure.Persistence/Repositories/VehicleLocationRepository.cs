using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class VehicleLocationRepository : IRepository<VehicleLocation>
    {
        private readonly AppDbContext _context;
        public VehicleLocationRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(VehicleLocation entity)
        {
            await _context.VehicleLocations.AddAsync(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

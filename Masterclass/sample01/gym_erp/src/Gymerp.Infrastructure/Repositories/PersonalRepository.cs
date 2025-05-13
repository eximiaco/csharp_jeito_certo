using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gymerp.Domain.Entities;
using Gymerp.Domain.Interfaces;
using Gymerp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Infrastructure.Repositories
{
    public class PersonalRepository : IPersonalRepository
    {
        private readonly ApplicationDbContext _context;

        public PersonalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Personal> GetByIdAsync(Guid id)
        {
            var personal = await _context.Personals.FindAsync(id);
            if (personal == null)
                throw new InvalidOperationException($"Personal with ID {id} not found.");
            return personal;
        }

        public async Task<Personal> GetByDocumentAsync(string document)
        {
            var personal = await _context.Personals.FirstOrDefaultAsync(p => p.Document == document);
            if (personal == null)
                throw new InvalidOperationException($"Personal with document {document} not found.");
            return personal;
        }

        public async Task<Personal> AddAsync(Personal personal)
        {
            await _context.Personals.AddAsync(personal);
            await _context.SaveChangesAsync();
            return personal;
        }

        public async Task<Personal> UpdateAsync(Personal personal)
        {
            _context.Personals.Update(personal);
            await _context.SaveChangesAsync();
            return personal;
        }
    }
} 
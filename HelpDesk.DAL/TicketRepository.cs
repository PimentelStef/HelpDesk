using HelpDesk.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.DAL
{
    public class TicketRepository : ITicketRepository
    {
        private readonly HelpDeskDbContext _context;

        public TicketRepository(HelpDeskDbContext context)
        {
            _context = context;
        }

        public List<Ticket> GetAll(string? status = null, int? categoryId = null, string? keyword = null)
        {
            var tickets = _context.Tickets
                .Include(m => m.Category)
                .Include(m => m.AssignedEmployee)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                tickets = tickets.Where(m => m.Status == status);

            if (categoryId.HasValue)
                tickets = tickets.Where(m => m.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(keyword))
                tickets = tickets.Where(m => m.IssueTitle.Contains(keyword) || 
                                             (m.AssignedEmployee != null && m.AssignedEmployee.FullName.Contains(keyword)));

            return tickets.ToList();
        }

        public void Add(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
        }

        public void Update(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
        }

        public Ticket Get(int id)
        {
            return _context.Tickets.Find(id);
        }

        public void Delete(int id)
        {
            _context.Tickets.Remove(Get(id));
        }

        public int Save()
        {
            return _context.SaveChanges();
        }
        public List<Ticket> GetFiltered(int? categoryId, string status)
        {
            var query = _context.Tickets.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            return query.ToList();
        }

    }
}

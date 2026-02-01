using HelpDesk.DAL;
using HelpDesk.DTO;
using HelpDesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.BLL
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;

        public TicketService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public List<DTO.Ticket> GetAll(string? status, int? category, string? keyword)
        {
            return _ticketRepository
                .GetAll(status, category, keyword)
                .Select(m => new DTO.Ticket 
                { 
                    Id = m.Id,
                    IssueTitle = m.IssueTitle,
                    Description = m.Description,
                    Category = m.Category.Name,
                    AssignedEmployee = m.AssignedEmployee.FullName,
                    Status = m.Status,
                    DateCreated = m.DateCreated
                })
                .ToList();
        }

        public (bool isOk, string message) Add(Model.Ticket ticket)
        {
            try
            {
                if (string.IsNullOrEmpty(ticket.IssueTitle))
                    return (false, "Title must not be empty!");

                if (ticket.CategoryId == null || ticket.CategoryId == 0)
                    return (false, "Category must be selected!");

                if (string.IsNullOrEmpty(ticket.Status))
                    return (false, "Status must be selected!");

                ticket.DateCreated = DateTime.Now;

                if (ticket.Status == "New")
                {
                    ticket.ResolutionNotes = null;
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "In-Progress")
                {
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "Resolved" || ticket.Status == "Closed")
                {
                    if (string.IsNullOrEmpty(ticket.ResolutionNotes))
                        return (false, "Resolution must not be empty!");

                    if (ticket.AssignedEmployeeId == null)
                        return (false, "Employee must be selected!");

                    ticket.DateResolved = DateTime.Now;

                    if (ticket.DateResolved < ticket.DateCreated)
                        return (false, "Date Resolved cannot be earlier than Date Created!");
                }

                _ticketRepository.Add(ticket);
                _ticketRepository.Save();

                return (true, "Ticket added successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding ticket: {ex.Message}");
            }
        }

        public (bool isOk, string message) Update(Model.Ticket ticket)
        {
            try
            {
                // Check if ticket exists in DB first
                var existingTicket = GetById(ticket.Id);
                if (existingTicket == null)
                    return (false, "Ticket does not exist or was removed.");

                // Validation (your existing rules)
                if (string.IsNullOrEmpty(ticket.IssueTitle))
                    return (false, "Title must not be empty!");

                if (ticket.CategoryId == null || ticket.CategoryId == 0)
                    return (false, "Category must be selected!");

                if (string.IsNullOrEmpty(ticket.Status))
                    return (false, "Status must be selected!");

                var validStatuses = new[] { "New", "In-Progress", "Resolved", "Closed" };
                if (!
                validStatuses.Contains(ticket.Status))
                    return (false, "Invalid ticket status!");

                // Preserve original DateCreated
                ticket.DateCreated = existingTicket.DateCreated;

                if (ticket.Status == "New")
                {
                    ticket.ResolutionNotes = null;
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "In-Progress")
                {
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "Resolved" || ticket.Status == "Closed")
                {
                    if (string.IsNullOrEmpty(ticket.ResolutionNotes))
                        return (false, "Resolution must not be empty!");

                    if (ticket.AssignedEmployeeId == null)
                        return (false, "Employee must be selected!");

                    ticket.DateResolved = DateTime.Now;

                    if (ticket.DateResolved < ticket.DateCreated)
                        return (false, "Date Resolved cannot be earlier than Date Created!");
                }

                // Call repository update
                _ticketRepository.Update(ticket);
                _ticketRepository.Save();

                return (true, "Ticket updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating ticket: {ex.Message}");
            }
        }

        public (bool isOk, string message) Delete(int ticketId)
        {
            try
            {
                var ticket = GetById(ticketId); // use new GetById method
                if (ticket == null)
                    return (false, "Ticket does not exist or already removed.");

                _ticketRepository.Delete(ticketId);
                _ticketRepository.Save();

                return (true, "Ticket deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting ticket: {ex.Message}");
            }
        }
        public (bool isOk, string message) DeleteAll()
        {
            try
            {
                var tickets = _ticketRepository.GetAll();
                if (tickets == null || tickets.Count == 0)
                    return (false, "No tickets to delete.");

                foreach (var t in tickets)
                {
                    _ticketRepository.Delete(t.Id);
                }

                _ticketRepository.Save();
                return (true, "All tickets cleared successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error clearing tickets: {ex.Message}");
            }
        }
        public Model.Ticket GetById(int ticketId)
        {
            // Ask the repository to find the ticket by its ID
            // Returns null if the ticket does not exist
            return _ticketRepository.GetAll().FirstOrDefault(t => t.Id == ticketId);
        }
        public List<Model.Ticket> GetAll()
        {
            return _ticketRepository.GetAll(); // return all tickets from repository
        }
        public List<Model.Ticket> GetFiltered(int? categoryId, string status)
        {
            return _ticketRepository.GetFiltered(categoryId, status);
        }
    }
}

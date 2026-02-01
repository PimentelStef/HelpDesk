using HelpDesk.BLL;
using HelpDesk.DAL;
using HelpDesk.Model;
using HelpDesk.DTO;

namespace HelpDesk.UI
{
    public partial class Form1 : Form
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public Form1(
            ITicketService ticketService,
            ITicketCategoryRepository ticketCategoryRepository,
            IEmployeeRepository employeeRepository)
        {
            InitializeComponent();
            _ticketService = ticketService;
            _ticketCategoryRepository = ticketCategoryRepository;
            _employeeRepository = employeeRepository;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDefaultValues();
            LoadTickets();
        }

        private void LoadDefaultValues()
        {
            cmbCategory.DataSource = _ticketCategoryRepository.GetAll();
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";

            cmbAssignedTo.DataSource = _employeeRepository.GetAll();
            cmbAssignedTo.DisplayMember = "FullName";
            cmbAssignedTo.ValueMember = "Id";

            cmbStatus.Items.AddRange(new string[] { "New", "In-Progress", "Resolved", "Closed" });
            cmbStatus.SelectedIndex = 0;
        }

        private void LoadTickets()
        {
            dgTickets.AutoGenerateColumns = true;
            dgTickets.DataSource = _ticketService.GetAll(null, null, null).ToList();
            dgTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgTickets.ReadOnly = true;
            dgTickets.AllowUserToAddRows = false;
        }

        private void btnCreateTicket_Click(object sender, EventArgs e)
        {
            Model.Ticket ticket = new Model.Ticket()
            {
                IssueTitle = txtIssueTitle.Text,
                Description = txtDescription.Text,
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = Convert.ToInt32(cmbAssignedTo.SelectedValue),
                Status = cmbStatus.Text
            };

            var result = _ticketService.Add(ticket);

            if (!result.isOk)
                MessageBox.Show(result.message);

            if (result.isOk)
            {
                MessageBox.Show(result.message);
                LoadDefaultValues();
                LoadTickets();
                return;
            }
        }

        private void dgTickets_SelectionChanged(object sender, EventArgs e)
        {
            if (dgTickets.CurrentRow?.DataBoundItem is DTO.Ticket sel)
            {
                txtIssueTitle.Text = sel.IssueTitle;
                txtDescription.Text = sel.Description;
            }
        }

        private void btnUpdateTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                lblStatus.Text = "No ticket selected.";
                return;
            }

            try
            {
                // Get the selected ticket ID from the DataGridView
                int ticketId = Convert.ToInt32(dgTickets.SelectedRows[0].Cells["Id"].Value);

                // Construct the updated ticket object
                Model.Ticket ticket = new Model.Ticket()
                {
                    Id = ticketId,
                    IssueTitle = txtIssueTitle.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                    AssignedEmployeeId = (int)(cmbAssignedTo.SelectedValue != null
                                         ? Convert.ToInt32(cmbAssignedTo.SelectedValue)
                                         : (int?)null), // allow null if nothing selected
                    Status = cmbStatus.Text.Trim(),
                    ResolutionNotes = string.IsNullOrWhiteSpace(txtResolution.Text)
                                      ? null
                                      : txtResolution.Text.Trim(),
                    DateCreated = DateTime.Now,
                };

                // Call your TicketService to update
                var result = _ticketService.Update(ticket);

                if (!result.isOk)
                {
                    MessageBox.Show(result.message);
                    lblStatus.Text = "Error updating ticket.";
                    return;
                }

                // Success
                MessageBox.Show(result.message);
                lblStatus.Text = "Ticket updated successfully.";

                // Refresh form values and ticket list
                LoadDefaultValues();
                LoadTickets();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error updating ticket: {ex.Message}";
            }
        }

        private void btnDeleleteTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                lblStatus.Text = "No ticket selected.";
                return;
            }

            if (!chkConfirmDelete.Checked)
            {
                lblStatus.Text = "Please check Confirm Delete to proceed.";
                return;
            }

            try
            {
                int ticketId = Convert.ToInt32(dgTickets.SelectedRows[0].Cells["Id"].Value);

                // Confirm deletion
                var confirm = MessageBox.Show(
                    "Are you sure you want to delete this ticket?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                    return;

                var result = _ticketService.Delete(ticketId);

                if (!result.isOk)
                {
                    MessageBox.Show(result.message);
                    lblStatus.Text = "Error deleting ticket (might be already removed).";
                    return;
                }

                lblStatus.Text = "Ticket deleted successfully.";
                LoadTickets();
                LoadDefaultValues();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error deleting ticket: {ex.Message}";
            }
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            if (dgTickets.Rows.Count == 0)
            {
                lblStatus.Text = "No tickets to clear.";
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete all tickets?",
                "Confirm Clear All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                // Loop through all tickets in the grid and delete
                foreach (DataGridViewRow row in dgTickets.Rows)
                {
                    int ticketId = Convert.ToInt32(row.Cells["Id"].Value);
                    var result = _ticketService.Delete(ticketId);

                    if (!result.isOk)
                    {
                        MessageBox.Show($"Error deleting ticket ID {ticketId}: {result.message}");
                    }
                }

                lblStatus.Text = "All tickets cleared successfully.";
                LoadTickets();
                LoadDefaultValues();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error clearing tickets: {ex.Message}";
            }
        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            try
            {
                int? categoryId = null;
                string status = null;

                // Category filter: All or valid CategoryId
                if (cmbFilterCategory.SelectedValue != null &&
                    Convert.ToInt32(cmbFilterCategory.SelectedValue) != 0)
                {
                    categoryId = Convert.ToInt32(cmbFilterCategory.SelectedValue);
                }

                // Status filter: All or valid status
                if (cmbFilterStatus.Text != "All")
                {
                    status = cmbFilterStatus.Text;
                }

                // ?? Fresh DB query
                var filteredTickets = _ticketService.GetFiltered(categoryId, status);

                dgTickets.DataSource = filteredTickets;

                lblCounts.Text = $"Visible: {filteredTickets.Count}";
                lblStatus.Text = "Filter applied.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error applying filter: {ex.Message}";
            }
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            try
            {
                cmbFilterCategory.SelectedIndex = 0; // All
                cmbFilterStatus.SelectedIndex = 0;   // All

                // Fresh DB reload
                LoadTickets();

                lblStatus.Text = "Filter reset!";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error resetting filter: {ex.Message}";
            }
        }
    }
}

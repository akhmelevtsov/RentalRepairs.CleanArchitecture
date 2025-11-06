# Phase 3: Complete JavaScript Implementation

Replace the current `@section Scripts` in `AssignWorker.cshtml` with the following enhanced version:

```razor
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
 // ========================================
        // Phase 3: Enhanced Booking Visibility JavaScript
        // ========================================
        
     // Global state
     let workersData = [];
 let currentRequestId = '@Model.AssignmentRequest.RequestId';
        let isEmergencyRequest = @(Model.AssignmentContext?.IsEmergencyRequest.ToString().ToLower() ?? "false");
        
        // ========================================
        // Initialization
        // ========================================
        document.addEventListener('DOMContentLoaded', function() {
      console.log('Phase 3: Initializing booking visibility features');
         initializeWorkersData();
        setupEventListeners();
  setupFormSubmission();
        });
        
        // ========================================
        // Data Loading
        // ========================================
    function initializeWorkersData() {
     const workerSelect = document.getElementById('workerSelect');
        if (!workerSelect) return;
            
            // Extract booking data from dropdown data attributes
            workersData = Array.from(workerSelect.options)
         .filter(opt => opt.value !== '')
           .map(opt => ({
           email: opt.value,
   name: opt.text,
 specialization: opt.dataset.specialization || '',
 bookedDates: opt.dataset.bookedDates 
            ? opt.dataset.bookedDates.split(',') 
     : [],
 partialDates: opt.dataset.partialDates 
              ? opt.dataset.partialDates.split(',') 
     : [],
           nextAvailable: opt.dataset.nextAvailable || null
       }));
            
        console.log('Phase 3: Loaded booking data for', workersData.length, 'workers');
            
            if (workersData.length > 0 && workersData[0].bookedDates.length > 0) {
                console.log('Sample worker booking data:', workersData[0]);
            }
        }
        
        // ========================================
        // Event Listeners
        // ========================================
        function setupEventListeners() {
  const workerSelect = document.getElementById('workerSelect');
     const dateSelect = document.getElementById('dateSelect');
  
if (workerSelect) {
  workerSelect.addEventListener('change', onWorkerChange);
     }
            
     if (dateSelect) {
        dateSelect.addEventListener('change', onDateChange);
}
        }
    
        // ========================================
     // Worker Selection Handler
        // ========================================
   function onWorkerChange() {
            const workerEmail = document.getElementById('workerSelect')?.value;
            
            if (!workerEmail) {
            resetQuickDateButtons();
         clearAvailabilityFeedback();
    return;
            }
        
      // Auto-generate work order number
autoGenerateWorkOrderNumber();
   
            // Update quick date buttons with booking indicators
            updateQuickDateButtons(workerEmail);
            
    // Validate current date selection
    const selectedDate = document.getElementById('dateSelect')?.value;
          if (selectedDate) {
            validateDateSelection(workerEmail, selectedDate);
            }
 }
      
     // ========================================
      // Date Selection Handler
   // ========================================
        function onDateChange() {
      const workerEmail = document.getElementById('workerSelect')?.value;
            const selectedDate = document.getElementById('dateSelect')?.value;
            
      if (!workerEmail || !selectedDate) return;
            
            validateDateSelection(workerEmail, selectedDate);
        }
        
 // ========================================
        // Quick Date Buttons Update
        // ========================================
        function updateQuickDateButtons(workerEmail) {
            const worker = workersData.find(w => w.email === workerEmail);
            if (!worker) return;
            
const quickDateBtns = document.querySelectorAll('.quick-date-btn');
        
  quickDateBtns.forEach(btn => {
     const btnDate = btn.dataset.date;
                const badge = btn.querySelector('.availability-badge');
   
                // Reset classes
     btn.classList.remove('btn-success', 'btn-warning', 'btn-danger', 'btn-outline-secondary');
   btn.disabled = false;
                
  if (worker.bookedDates.includes(btnDate)) {
     // Fully booked (2/2 slots)
         if (isEmergencyRequest) {
           // Emergency can override
     btn.classList.add('btn-warning');
     badge.textContent = '?? 2/2 (Emergency OK)';
      badge.className = 'availability-badge badge bg-warning text-dark ms-1';
  } else {
       // Normal request - blocked
         btn.classList.add('btn-danger');
 btn.disabled = true;
           badge.textContent = '? Fully Booked';
            badge.className = 'availability-badge badge bg-danger ms-1';
        }
       badge.style.display = 'inline';
                } else if (worker.partialDates.includes(btnDate)) {
     // Partially booked (1/2 slots)
       btn.classList.add('btn-warning');
  badge.textContent = '?? 1/2 Available';
badge.className = 'availability-badge badge bg-warning text-dark ms-1';
         badge.style.display = 'inline';
    } else {
            // Fully available (0/2 slots)
      btn.classList.add('btn-success');
       badge.textContent = '? Available';
          badge.className = 'availability-badge badge bg-success ms-1';
      badge.style.display = 'inline';
          }
            });
        }
        
 // ========================================
        // Date Selection Validation
        // ========================================
        function validateDateSelection(workerEmail, selectedDate) {
            const worker = workersData.find(w => w.email === workerEmail);
     if (!worker) return;
        
          const availabilitySection = document.getElementById('availabilitySection');
 const availabilityContainer = document.getElementById('availabilityContainer');
    const assignButton = document.getElementById('assignButton');
    
      if (!availabilitySection || !availabilityContainer || !assignButton) return;
        
        availabilitySection.style.display = 'block';
            
    if (worker.bookedDates.includes(selectedDate)) {
 // Fully booked (2/2 slots)
       if (isEmergencyRequest) {
            availabilityContainer.innerHTML = `
          <div class="alert alert-warning">
       <i class="fas fa-exclamation-triangle me-2"></i>
 <strong>Warning: Worker Already Booked</strong><br>
        This worker already has 2 assignments on this date. 
        <strong>Emergency override enabled</strong> - assignment will proceed 
    but worker capacity will be exceeded.
       </div>
`;
    assignButton.disabled = false;
         } else {
       availabilityContainer.innerHTML = `
      <div class="alert alert-danger">
  <i class="fas fa-times-circle me-2"></i>
           <strong>Worker Not Available</strong><br>
         This worker already has 2 assignments on this date (maximum capacity reached).
    Please select a different date or worker.
  </div>
       `;
    assignButton.disabled = true;
         }
            } else if (worker.partialDates.includes(selectedDate)) {
     // Partially booked (1/2 slots)
                availabilityContainer.innerHTML = `
      <div class="alert alert-info">
           <i class="fas fa-info-circle me-2"></i>
        <strong>Limited Availability</strong><br>
This worker has 1 of 2 assignment slots filled on this date. 
  Assignment can proceed.
  </div>
     `;
    assignButton.disabled = false;
      } else {
     // Fully available (0/2 slots)
    availabilityContainer.innerHTML = `
<div class="alert alert-success">
    <i class="fas fa-check-circle me-2"></i>
        <strong>Worker Available</strong><br>
   This worker has no assignments on this date. Full availability.
    </div>
       `;
   assignButton.disabled = false;
  }
        }
        
        // ========================================
   // Quick Date Button Click Handler
        // ========================================
    function selectQuickDate(button) {
            if (button.disabled) return;
        
          const dateSelect = document.getElementById('dateSelect');
       if (dateSelect) {
         dateSelect.value = button.dataset.date;
              onDateChange();
         }
        }
   
  // ========================================
        // Reset Quick Date Buttons
        // ========================================
        function resetQuickDateButtons() {
            const quickDateBtns = document.querySelectorAll('.quick-date-btn');
            quickDateBtns.forEach(btn => {
          btn.classList.remove('btn-success', 'btn-warning', 'btn-danger');
       btn.classList.add('btn-outline-secondary');
btn.disabled = false;
                const badge = btn.querySelector('.availability-badge');
        if (badge) badge.style.display = 'none';
          });
        }
        
    // ========================================
        // Clear Availability Feedback
  // ========================================
     function clearAvailabilityFeedback() {
            const availabilitySection = document.getElementById('availabilitySection');
            if (availabilitySection) {
          availabilitySection.style.display = 'none';
     }
        }
   
        // ========================================
      // Auto-Generate Work Order Number
        // ========================================
 function autoGenerateWorkOrderNumber() {
    const workOrderField = document.querySelector('input[name="AssignmentRequest.WorkOrderNumber"]');
            if (workOrderField && workOrderField.value === '') {
          const today = new Date();
          const dateStr = today.getFullYear() + '' + 
         (today.getMonth() + 1).toString().padStart(2, '0') + '' +
          today.getDate().toString().padStart(2, '0');
                const randomNum = Math.floor(Math.random() * 1000).toString().padStart(3, '0');
            workOrderField.value = `WO-${dateStr}-${randomNum}`;
       }
        }
        
   // ========================================
   // Form Submission
 // ========================================
        function setupFormSubmission() {
            const form = document.querySelector('form[method="post"]');
            const submitBtn = document.getElementById('assignButton');
            
   if (form && submitBtn) {
  form.addEventListener('submit', function(e) {
          // Validate required fields
    const workerEmail = document.getElementById('workerSelect')?.value;
        const scheduledDate = document.getElementById('dateSelect')?.value;
           const workOrderNumber = document.querySelector('input[name="AssignmentRequest.WorkOrderNumber"]')?.value;
     
                    if (!workerEmail || !scheduledDate || !workOrderNumber) {
             e.preventDefault();
      alert('Please fill in all required fields.');
             return false;
   }
           
         // Disable submit button to prevent double submission
 submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Assigning Worker...';
     });
 }
        }
    </script>
}
```

## Key Features Implemented:

1. **Worker Booking Data Loading**
   - Reads `data-booked-dates` and `data-partial-dates` from dropdown options
   - No AJAX needed - all data loaded on page load

2. **Quick Date Button Enhancement**
   - Green (? Available) - 0/2 slots filled
   - Yellow (?? 1/2 Available) - 1/2 slots filled
   - Red (? Fully Booked) - 2/2 slots filled
   - Yellow with override (?? 2/2 Emergency OK) - Emergency can proceed

3. **Real-Time Validation**
   - Shows availability status when date selected
   - Blocks assignment if worker fully booked (normal requests)
   - Allows with warning if emergency override

4. **Emergency Override Handling**
   - Yellow buttons instead of red for emergency requests
   - Warning message but allows assignment
   - Clear visual feedback

5. **UX Improvements**
   - Auto-generates work order numbers
   - Disables submit during processing
   - Clear visual feedback at every step

## Installation:
Copy the entire `@section Scripts` block above and replace the existing one in `AssignWorker.cshtml`.

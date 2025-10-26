# PeerPay Application - Complete Integration Guide

## Overview
This document describes the complete end-to-end integration of job posting, application, and messaging features in the PeerPay application.

## Complete User Flows

### 1. Employer Flow: Post Job → Receive Applications → Message Students

#### Step 1: Post a Job
**Component:** `PostJob.tsx` (`src/components/employer/PostJob.tsx`)
- **Route:** `/employer/jobs/create`
- **Access:** Employer clicks "Post New Job" button from Dashboard
- **Process:**
  1. Employer fills out job posting form with:
     - Title, Description
     - Category, Pay Amount, Pay Type
     - Duration, Required Skills
     - Deadline, Location, Job Type
     - Max Applicants
  2. Form validation with error feedback
  3. Calls `jobService.createJob(formData)`
  4. Redirects to Employer Dashboard on success

#### Step 2: View Applications
**Component:** `ManageApplications.tsx` (`src/components/employer/ManageApplications.tsx`)
- **Route:** `/employer/jobs/:jobId/applications`
- **Access:** 
  - Click "View Applications" button on any job in Employer Dashboard
  - Shows application count for each job
- **Process:**
  1. Fetches all applications for the job using `jobService.getJobApplications(jobId)`
  2. For each application, fetches student details via `/api/student/:studentId`
  3. Displays applications in a list/detail view with:
     - Student name, email, rating, completed jobs count
     - Application status (Pending/Accepted/Rejected)
     - Cover letter and attachments
     - Application date
  4. Filters: All, Pending, Accepted, Rejected
  5. Select any application to view full details

#### Step 3: Review and Accept/Reject
**Component:** `ManageApplications.tsx` - Action Buttons
- **Actions Available:**
  - **Accept Application:** 
    - Calls `jobService.updateApplicationStatus({ applicationId, status: 'Accepted', feedback })`
    - Sends confirmation message to student
  - **Reject Application:**
    - Calls `jobService.updateApplicationStatus({ applicationId, status: 'Rejected', feedback })`
    - Sends notification to student
  - **Message Student:** (See Step 4)

#### Step 4: Message Student
**Component:** `ManageApplications.tsx` - handleMessage function
- **Process:**
  1. Gets current employer user from `useAuthStore()`
  2. Creates conversation via `POST /api/conversation` with:
     ```json
     {
       "participant1Id": "employer-user-id",
       "participant2Id": "student-user-id"
     }
     ```
  3. If conversation already exists, returns existing conversation
  4. Navigates to `/messages/:conversationId` with state containing student info
  5. Opens `ChatInterface` component for real-time messaging

---

### 2. Student Flow: Browse Jobs → Apply → Receive Messages

#### Step 1: Browse Jobs
**Component:** `JobBoard.tsx` (`src/components/student/JobBoard.tsx`)
- **Route:** `/student/jobs`
- **Access:** Student Dashboard or navigation menu
- **Features:**
  1. Search by keywords, location
  2. Filter by:
     - Category
     - Job Types (FullTime, PartTime, ProjectBased, Freelance)
     - Pay Types (Hourly, Daily, Weekly, Monthly, Fixed)
     - Budget range
     - Required skills
  3. Displays jobs in card format with:
     - Title, Description
     - Pay amount and type
     - Location, Duration
     - Application deadline
     - Save/Unsave functionality
  4. Click any job card to view details

#### Step 2: View Job Details & Apply
**Component:** `JobDetails.tsx` (`src/components/student/JobDetails.tsx`)
- **Route:** `/student/jobs/:id`
- **Process:**
  1. Fetches job details using `jobService.getJobById(id)`
  2. Checks if student has already applied using `jobService.checkIfApplied()`
  3. Displays:
     - Full job description
     - Employer information
     - Pay details and duration
     - Required skills
     - Application deadline
     - Number of applicants
  4. **Apply Button** (if not already applied):
     - Opens application modal
     - Student enters:
       - Cover letter (required, 50+ characters)
       - Attachments (optional)
     - Submits via `jobService.applyForJob()`:
       ```typescript
       {
         jobId: string,
         studentId: string,
         coverLetter: string,
         attachments?: string[]
       }
       ```
  5. Shows "Applied" status if already applied
  6. Option to withdraw application

#### Step 3: Check Application Status
**Component:** `StudentDashboard.tsx` (`src/components/student/StudentDashboard.tsx`)
- **Route:** `/student/dashboard`
- **Displays:**
  - My Applications section
  - Application status for each job:
    - Submitted (yellow)
    - UnderReview (yellow)
    - Shortlisted (blue)
    - Selected (green)
    - Rejected (red)
    - Withdrawn (gray)
  - Click on application to view details

#### Step 4: Receive Messages from Employer
**Component:** `ConversationList.tsx` & `ChatInterface.tsx`
- **Route:** `/messages` and `/messages/:conversationId`
- **Access:** Click "Messages" icon in navigation
- **Process:**
  1. `ConversationList` shows all conversations with:
     - Other participant name
     - Last message preview
     - Unread message count
     - Auto-refresh every 30 seconds
  2. Click conversation to open `ChatInterface`
  3. Real-time messaging with:
     - Message history (paginated)
     - Send new messages
     - Read receipts
     - Polling every 5 seconds for new messages
     - Auto-scroll to latest message
  4. Click back arrow to return to conversation list

---

## Technical Integration Details

### API Endpoints Used

#### Job Management
```
POST   /api/job                              - Create job
GET    /api/job/{id}                        - Get job details
PUT    /api/job/{id}                        - Update job
DELETE /api/job/{id}                        - Delete job
GET    /api/job/employer/{employerId}       - Get employer's jobs
GET    /api/job/search                      - Search jobs
```

#### Job Applications
```
POST   /api/jobapplication                  - Apply for job
GET    /api/jobapplication/job/{jobId}      - Get applications for job
GET    /api/jobapplication/student/{studentId} - Get student's applications
PUT    /api/jobapplication/{id}/status      - Update application status
DELETE /api/jobapplication/{id}             - Withdraw application
```

#### Messaging
```
POST   /api/conversation                    - Create conversation
GET    /api/conversation/user/{userId}      - Get user's conversations
GET    /api/conversation/{id}               - Get conversation details
POST   /api/message                         - Send message
GET    /api/message/conversation/{conversationId} - Get messages
PUT    /api/message/{id}/read               - Mark message as read
PUT    /api/message/conversation/{conversationId}/read - Mark all as read
```

#### Student Details
```
GET    /api/student/{studentId}             - Get student profile
```

### Authentication Integration

**Auth Store:** `src/store/authStore.ts` (Zustand)
```typescript
interface AuthState {
  user: User | null;
  token: string | null;
  login: (user: User, token: string) => void;
  logout: () => void;
}

// User object contains:
{
  userId: string;
  name: string;
  email: string;
  userType: 'Student' | 'Employer' | 'Admin';
  // ... other fields
}
```

**Usage in Components:**
```typescript
const { user } = useAuthStore();

// Access user data
const userId = user?.userId;
const userName = user?.name;
const userType = user?.userType;

// Check authentication
if (!user) {
  navigate('/login');
}
```

### Component Props and Data Flow

#### ManageApplications Component
**Props:** None (uses URL params)
```typescript
const { jobId } = useParams<{ jobId: string }>();
const { user } = useAuthStore(); // Employer user
```

**State:**
```typescript
const [applications, setApplications] = useState<ApplicationWithStudent[]>([]);
const [selectedApp, setSelectedApp] = useState<ApplicationWithStudent | null>(null);
const [filter, setFilter] = useState<'all' | 'Pending' | 'Accepted' | 'Rejected'>('all');
```

**Key Functions:**
- `fetchApplications()` - Loads applications and student details
- `handleUpdateStatus(applicationId, status)` - Accept/Reject application
- `handleMessage()` - Create conversation and navigate to chat

#### ChatInterface Component
**Props:**
```typescript
interface ChatInterfaceProps {
  currentUserId: string;
  currentUserName: string;
}
```

**Provided by:** `ChatPage.tsx` using `useAuthStore()`

#### ConversationList Component
**Props:**
```typescript
interface ConversationListProps {
  currentUserId: string;
  userType: 'student' | 'employer';
}
```

**Provided by:** `MessagesPage.tsx` using `useAuthStore()`

### Navigation Flow

```
Employer Dashboard
  ├─ Click "Post New Job" → PostJob → Create Job → Back to Dashboard
  ├─ Click "View Applications" on Job → ManageApplications
  │    ├─ Select Application → View Details
  │    ├─ Click "Accept" → Update Status → Refresh
  │    ├─ Click "Reject" → Update Status → Refresh
  │    └─ Click "Message Student" → Create Conversation → ChatInterface
  └─ Click Messages Icon → ConversationList → Select Conversation → ChatInterface

Student Dashboard
  ├─ Click "Browse Jobs" → JobBoard → Search/Filter
  │    └─ Click Job Card → JobDetails
  │         ├─ Click "Apply" → Application Modal → Submit → Success
  │         └─ Click "Withdraw" → Confirm → Application Withdrawn
  ├─ View "My Applications" → Status Updates
  └─ Click Messages Icon → ConversationList → Select Conversation → ChatInterface
```

### Routes Configuration (App.tsx)

```typescript
// Student Routes
<Route path="/student/dashboard" element={<StudentDashboard />} />
<Route path="/student/jobs" element={<JobBoard />} />
<Route path="/student/jobs/:id" element={<JobDetails />} />

// Employer Routes
<Route path="/employer/dashboard" element={<EmployerDashboard />} />
<Route path="/employer/jobs/create" element={<PostJob />} />
<Route path="/employer/jobs/:jobId/applications" element={<ManageApplications />} />

// Messaging Routes (Shared)
<Route path="/messages" element={<MessagesPage />} />
<Route path="/messages/:conversationId" element={<ChatPage />} />
```

### Error Handling

**Pattern Used Throughout:**
```typescript
try {
  const result = await someService.someMethod();
  toast.success('Operation successful!');
} catch (error: any) {
  console.error('Operation failed:', error);
  toast.error(error.response?.data?.message || 'Operation failed');
}
```

**Loading States:**
```typescript
const [loading, setLoading] = useState(true);

if (loading) {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#8C00FF]"></div>
    </div>
  );
}
```

### Styling and UI

**Technology:** Tailwind CSS
**Theme Colors:**
- Primary: `#8C00FF` (Purple)
- Secondary: `#7300CC` (Darker Purple)
- Success: Green variants
- Error: Red variants
- Warning: Yellow variants

**Status Color Coding:**
```typescript
// Application Status
Pending/Submitted → Yellow (bg-yellow-100 text-yellow-800)
Shortlisted → Blue (bg-blue-100 text-blue-800)
Accepted/Selected → Green (bg-green-100 text-green-800)
Rejected → Red (bg-red-100 text-red-800)

// Job Status
Active → Green
Closed → Gray
Completed → Purple
```

**Responsive Design:**
- Mobile-first approach
- Grid layouts: `grid-cols-1 lg:grid-cols-3`
- Breakpoints: `sm:`, `md:`, `lg:`, `xl:`

---

## Testing the Complete Flow

### Employer Testing Checklist
1. ✅ Login as employer
2. ✅ Post a new job from dashboard
3. ✅ View job in "My Job Posts" section
4. ✅ Wait for student applications (or create test applications)
5. ✅ Click "View Applications" on job
6. ✅ See list of applications with student details
7. ✅ Filter by Pending/Accepted/Rejected
8. ✅ Select an application to view details
9. ✅ Click "Accept" → Verify status updates
10. ✅ Click "Message Student" → Verify conversation created
11. ✅ Send message in chat → Verify message sent
12. ✅ Receive reply from student → Verify message appears

### Student Testing Checklist
1. ✅ Login as student
2. ✅ Browse jobs from dashboard
3. ✅ Use search and filters to find jobs
4. ✅ Click on a job to view details
5. ✅ Click "Apply" → Fill cover letter → Submit
6. ✅ Verify "Applied" status appears
7. ✅ Check "My Applications" in dashboard
8. ✅ Verify application status shows "Pending"
9. ✅ Wait for employer message (or employer accepts application)
10. ✅ Click Messages icon → See conversation
11. ✅ Open conversation → Read employer's message
12. ✅ Reply to message → Verify message sent
13. ✅ Check unread count updates correctly

### Integration Points to Verify
- ✅ User authentication persists across routes
- ✅ Job ID correctly passed to applications page
- ✅ Student ID correctly fetched from application
- ✅ Conversation created with correct participant IDs
- ✅ Messages appear in real-time (within 5 seconds)
- ✅ Read receipts work correctly
- ✅ Application status updates reflect immediately
- ✅ Navigation between components works smoothly
- ✅ Back buttons return to correct previous pages
- ✅ Toast notifications appear for success/error

---

## Future Enhancements

### Planned Features
1. **Real-time Notifications**
   - WebSocket integration for instant notifications
   - Push notifications for new messages
   - Application status change alerts

2. **Payment Integration**
   - Connect payment system to completed jobs
   - Escrow service for job payments
   - Payment history and invoicing

3. **Enhanced Messaging**
   - File attachments in messages
   - Image/video sharing
   - Voice messages
   - Emoji reactions

4. **Application Enhancements**
   - Batch accept/reject applications
   - Application notes for employers
   - Interview scheduling
   - Application analytics

5. **Job Enhancements**
   - Job templates for employers
   - Recommended jobs for students
   - Job alerts and saved searches
   - Advanced matching algorithm

6. **User Profiles**
   - Portfolio/work samples
   - Detailed student profiles
   - Employer company pages
   - Reviews and ratings display

---

## Troubleshooting

### Common Issues

**Issue:** Applications not showing up
- **Check:** Verify job ID in URL is correct
- **Check:** Ensure backend API is running (https://localhost:7255)
- **Check:** Check browser console for API errors
- **Fix:** Verify database has application records

**Issue:** Messages not appearing
- **Check:** Conversation ID is valid
- **Check:** User is authenticated
- **Check:** Backend messaging endpoints are working
- **Fix:** Check polling interval (should be 5 seconds)

**Issue:** "Message Student" button not working
- **Check:** User is logged in as employer
- **Check:** Student ID is correct in application
- **Check:** Conversation API endpoint responds
- **Fix:** Check browser network tab for API errors

**Issue:** Cannot apply to job
- **Check:** Student is logged in
- **Check:** Job is still active
- **Check:** Student hasn't already applied
- **Check:** Cover letter meets minimum length (50 chars)
- **Fix:** Check validation error messages

### Debug Mode

**Enable Detailed Logging:**
```typescript
// Add to any service call
console.log('Request:', payload);
console.log('Response:', response);
console.log('Error:', error);
```

**Check API Responses:**
- Open browser DevTools → Network tab
- Filter by "Fetch/XHR"
- Check status codes (200 = success, 400 = bad request, 401 = unauthorized, 500 = server error)
- View response payloads

**Verify Authentication:**
```typescript
// In component
const { user } = useAuthStore();
console.log('Current User:', user);
console.log('User ID:', user?.userId);
console.log('User Type:', user?.userType);
```

---

## Support

For issues or questions:
1. Check this integration guide
2. Review component code and comments
3. Check browser console for errors
4. Verify backend API is running
5. Check database records

## Conclusion

The PeerPay application now has complete end-to-end integration for:
- ✅ Job posting by employers
- ✅ Job browsing and application by students
- ✅ Application management by employers
- ✅ Real-time messaging between students and employers
- ✅ Authentication throughout the flow
- ✅ Proper navigation and routing
- ✅ Error handling and user feedback

All components are connected and functional. Test the complete flow to ensure everything works as expected!

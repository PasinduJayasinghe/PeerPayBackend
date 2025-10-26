 # PeerPay - Database Integration Guide

This guide will help you integrate the mock data with your application and test the complete data flow.

## Step 1: Run the Database Mock Data Script

1. **Open SQL Server Management Studio (SSMS)** or your preferred SQL client

2. **Connect to your database server**

3. **Open the mock data script**:
   - Navigate to: `PeerPayBackend\Database\CompleteData_Insert.sql`

4. **Update the database name** (Line 7):
   ```sql
   USE [PeerPayDB]; -- Change this to your actual database name
   ```

5. **Execute the script**:
   - Select all content (Ctrl+A)
   - Click Execute (F5)
   - Wait for completion message

6. **Verify the data**:
   ```sql
   -- Check if data was inserted
   SELECT COUNT(*) FROM Users;           -- Should return 11
   SELECT COUNT(*) FROM JobCategories;   -- Should return 12
   SELECT COUNT(*) FROM Jobs;            -- Should return 6
   SELECT COUNT(*) FROM Students;        -- Should return 5
   SELECT COUNT(*) FROM Employers;       -- Should return 3
   ```

## Step 2: Start the Backend API

1. **Open Terminal in the backend project directory**:
   ```powershell
   cd C:\Users\thisa\OneDrive\Desktop\Peerpay\PeerPayBackend\PeerPayBackend
   ```

2. **Build the project** (if needed):
   ```powershell
   dotnet build
   ```

3. **Run the API**:
   ```powershell
   dotnet run
   ```

4. **Verify the API is running**:
   - You should see: `Now listening on: https://localhost:7255`
   - Open Swagger: `https://localhost:7255/swagger`

5. **Test the Job Category endpoint**:
   - In Swagger, find `GET /api/jobcategory`
   - Click "Try it out" → "Execute"
   - You should see 12 categories returned

6. **Test the Jobs endpoint**:
   - Find `GET /api/job` (Get all active jobs)
   - Click "Try it out" → "Execute"
   - You should see 3 active jobs returned (JOB001, JOB002, JOB003, JOB006)

## Step 3: Start the Frontend

1. **Open a new Terminal in the frontend directory**:
   ```powershell
   cd C:\Users\thisa\OneDrive\Desktop\Peerpay\PeerPayBackend\peerpayfrontend
   ```

2. **Install dependencies** (if needed):
   ```powershell
   npm install
   ```

3. **Start the development server**:
   ```powershell
   npm run dev
   ```

4. **Open the application**:
   - Navigate to: `http://localhost:5173`

## Step 4: Test the Data Integration

### Home Page Testing

1. **Open the Home page** (`http://localhost:5173`)

2. **Verify Categories Section**:
   - Should display 12 job categories from database
   - Each category shows job count
   - Clicking a category should navigate to filtered jobs

3. **Verify Featured Jobs Section**:
   - Should display up to 6 active jobs from database
   - Each job shows:
     - Title, Budget (Rs format)
     - Required skills
     - Duration, Posted date
     - Application count
   - Clicking "View Details" should navigate to job details

4. **Check for Loading States**:
   - Refresh the page
   - You should see skeleton loaders while data is fetching

### Student Dashboard Testing

1. **Login as a Student**:
   - Email: `kasun.perera@student.uom.lk`
   - Password: `Student@123`

2. **Verify Dashboard Data**:
   - Stats cards should show real data
   - Recent jobs should display from database
   - Applications section shows student's applications

3. **Navigate to Job Board**:
   - Click "Browse Jobs" or visit `/student/jobs`
   - Should see all active jobs
   - Filter and search should work

### Employer Dashboard Testing

1. **Login as an Employer**:
   - Email: `hr@techstartup.lk`
   - Password: `Employer@123`

2. **Verify Dashboard Data**:
   - My Jobs section shows employer's posted jobs (JOB001, JOB004)
   - Each job shows application count
   - Stats show real numbers from database

3. **Test Job Management**:
   - View job details
   - See applications received
   - Check applicant information

### Admin Dashboard Testing

1. **Login as Admin**:
   - Email: `admin@peerpay.lk`
   - Password: `Admin@123`

2. **Verify Admin Features**:
   - View all users
   - View all jobs
   - View system statistics

## Step 5: Common Issues and Solutions

### Issue: "Network Error" or "Failed to fetch"

**Solution**:
- Ensure backend is running on `https://localhost:7255`
- Check `.env.development` file has correct API URL
- Verify CORS is enabled in `Program.cs`

### Issue: "404 Not Found" for API endpoints

**Solution**:
- Rebuild the backend project
- Ensure all controllers are registered
- Check controller routing attributes

### Issue: No data showing on frontend

**Solution**:
- Open Browser DevTools (F12) → Console tab
- Check for API error messages
- Verify data was inserted in database
- Test API endpoints directly in Swagger

### Issue: SQL Script fails

**Solution**:
- Ensure database name is correct
- Check if tables exist (run migrations first)
- Verify foreign key constraints are satisfied
- Run script in sections to identify problem area

## Step 6: Data Available for Testing

### Test Accounts

**Admins:**
- `admin@peerpay.lk` / Admin@123 (SuperAdmin)
- `moderator@peerpay.lk` / Admin@123 (Moderator)
- `support@peerpay.lk` / Admin@123 (Support)

**Students:**
- `kasun.perera@student.uom.lk` / Student@123
- `nimali.silva@ucsc.cmb.ac.lk` / Student@123
- `ravindu.fernando@sliit.lk` / Student@123
- `thisara.j@pdn.ac.lk` / Student@123
- `dinuka.w@uom.lk` / Student@123

**Employers:**
- `hr@techstartup.lk` / Employer@123
- `contact@digitalhub.lk` / Employer@123
- `hiring@shopceylon.lk` / Employer@123

### Sample Data Overview

- **12 Job Categories**: Web Dev, Mobile, Design, Writing, etc.
- **6 Jobs**: 3 Active, 1 Completed, 1 Closed, 1 Active (Data Analysis)
- **6 Job Applications**: Various statuses (Pending, Accepted, Completed)
- **3 Conversations**: Between employers and students
- **5 Messages**: Sample conversation exchanges
- **2 Payments**: 1 Completed, 1 Escrowed
- **2 Ratings**: Bidirectional ratings
- **5 Earnings Records**: For each student
- **4 Notifications**: Application updates, messages, payments
- **3 Withdrawal Requests**: Various statuses

## Step 7: Next Steps

1. **Test Complete User Flows**:
   - Student applies to job → Employer accepts → Payment → Rating
   - Employer posts job → Receives applications → Selects candidate
   - Admin monitors → Views reports → Manages users

2. **Test Edge Cases**:
   - Empty states (no jobs, no applications)
   - Error handling (network errors, validation errors)
   - Loading states

3. **Performance Testing**:
   - Test with larger datasets
   - Check pagination
   - Verify search and filter performance

4. **UI/UX Testing**:
   - Mobile responsiveness
   - Cross-browser compatibility
   - Accessibility features

## Useful SQL Queries for Testing

```sql
-- View all active jobs with employer info
SELECT j.Title, j.PayAmount, e.CompanyName, j.Status, j.ApplicationCount
FROM Jobs j
JOIN Employers e ON j.EmployerId = e.EmployerId
WHERE j.Status = 0; -- Active

-- View applications for a specific job
SELECT s.User.Name AS StudentName, ja.Status, ja.AppliedDate
FROM JobApplications ja
JOIN Students s ON ja.StudentId = s.StudentId
WHERE ja.JobId = 'JOB001';

-- View student earnings
SELECT u.Name, e.TotalEarnings, e.AvailableBalance, e.PendingAmount
FROM Earnings e
JOIN Students s ON e.StudentId = s.StudentId
JOIN Users u ON s.UserId = u.UserId;

-- View conversation messages
SELECT m.Content, u.Name AS Sender, m.Timestamp
FROM Messages m
JOIN Users u ON m.SenderId = u.UserId
WHERE m.ConversationId = 'CONV001'
ORDER BY m.Timestamp;
```

## Support

If you encounter any issues:
1. Check the console logs (both frontend and backend)
2. Verify database connections
3. Ensure all dependencies are installed
4. Check API endpoint URLs and CORS settings

---

**Last Updated**: October 26, 2025
**Version**: 1.0

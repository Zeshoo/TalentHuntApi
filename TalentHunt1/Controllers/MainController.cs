using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TalentHunt1.Models;
using Newtonsoft.Json;
using System.Drawing.Text;
using System.Data.Entity.Core;
using System.Runtime.CompilerServices;


namespace TalentHunt1.Controllers
{
    public class MainController : ApiController
    {
        Talent_HuntEntities5 db = new Talent_HuntEntities5();

        [HttpPost]
        public HttpResponseMessage AddUser(Users user)
        {
            try
            {
                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid user data.");
                }

                var existingUser = db.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Email already registered.");
                }

                db.Users.Add(user);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "User registered successfully.");
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + e.Message);
            }
        }
        [HttpGet]
        public async Task<HttpResponseMessage> getUser()
        {
            try
            {
                var data = await db.Users.ToListAsync();
                
                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("api/Main/Signup")]
        public HttpResponseMessage Signup(Users user)
        {
            try
            {
                if (user == null || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid user data.");
                }

                // Check for duplicate email
                var existingUser = db.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Email already registered.");
                }

                // Set default role to Student
                user.Role = "Student";

                db.Users.Add(user);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "User registered successfully.");
            }
            catch (Exception e)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + e.Message);
            }
        }
        [HttpPost]
        public async Task<IHttpActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Email and password are required.");
            }

            List<Users> users = new List<Users>();

            using (HttpClient client = new HttpClient())
            {
                // Replace with your actual API URL to fetch users
                HttpResponseMessage response = await client.GetAsync("your_api_url_here");

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    users = JsonConvert.DeserializeObject<List<Users>>(jsonData);
                }
            }

            // Check if the user exists with the provided email and password
            Users loggedInUser = users.FirstOrDefault(u => u.Email == email && u.Password == password);

            if (loggedInUser != null)
            {
                // Return user data or a success response
                var userResponse = new
                {
                    UserId = loggedInUser.Id,
                    UserRole = loggedInUser.Role,
                    UserName = loggedInUser.Name
                };

                return Ok(userResponse);
            }

            return Unauthorized(); // Return unauthorized if login fails
        }
        [HttpGet]
        public HttpResponseMessage GetCommitteeMember()
        {
            try
            {
                var data = from user in db.CommitteeMember
                           select new
                           {
                               Id = user.Id,
                               Name = user.Name,
                               selectvalue = false

                           };
                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
        [HttpGet]
        public async Task<IHttpActionResult> ShowAllEvents()
        {
            try
            {
                var data = await db.Event.ToListAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
       
        public async Task<HttpResponseMessage> ShowTask(int eventid)
        {
            try
            {
                var data = await db.Task
                                   .Where(x => x.EventID == eventid)
                                   .ToListAsync();

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No tasks found for the given Event ID.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
       
        public HttpResponseMessage TaskDetails(int id)
        {
            try
            {
                using (var db = new Talent_HuntEntities5())
                {
                    var task = db.Task.FirstOrDefault(t => t.Id == id);
                    if (task == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No tasks found for the given Task ID.");
                    }

                    var result = new
                    {
                        task.Id,
                        task.EventID,
                        task.Description,
                        TaskStartTime = task.TaskStartTime,
                        TaskEndTime = task.TaskEndTime,
                        Image =task.Image 
                    };

                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage ViewSubmissions(int taskId)
        {
            try
            {
                using (var db = new Talent_HuntEntities5())
                {
                    var submissions = (from s in db.Submission
                                       join u in db.Users on s.UserID equals u.Id
                                       join t in db.Task on s.TaskID equals t.Id
                                       join e in db.Event on t.EventID equals e.Id
                                       join m in db.Marks on s.Id equals m.SubmissionID into marksGroup
                                       from m in marksGroup.DefaultIfEmpty()
                                       where s.TaskID == taskId
                                       select new
                                       {
                                           SubmissionID =s.Id,
                                           s.TaskID,
                                           s.UserID,
                                           UserName = u.Name,
                                           s.SubmissionTime,
                                           s.PathofSubmission,
                                           EventTitle = e.Title,
                                           Details = t.Description,
                                           Marks = m != null ? m.Marks1 : (int?)null  // 👈 Use actual property name here
                                       }).ToList();

                    if (submissions == null || submissions.Count == 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No submissions found for the given Task ID.");
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, submissions);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }






        //public class Ruletable
        //{
        //    public int Id { get; set; }
        //    public int EventID { get; set; }
        //    public List<string> Rules { get; set; }
        //}

        //[HttpPost]
        //public HttpResponseMessage AddRules(Ruletable val)
        //{
        //    try
        //    {
        //        foreach (var rule in val.Rules)
        //        {
        //            var ruleEntity = new Rules
        //            {
        //                EventID = val.EventID,  // Ensure the EventID is set
        //                Description = rule      // Assign rule description properly
        //            };

        //            db.Rules.Add(ruleEntity);
        //            db.SaveChanges();
        //        }

        //        // Save all rules to the database

        //        return Request.CreateResponse(HttpStatusCode.OK, "Rules added successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        public class AssignedCommitteeMember
        {
            public int Id { get; set; }
            public int EventId { get; set; }
            public List<int> MemberIdList { get; set; }
            public string Status { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage AssignedMemberToEvent(AssignedCommitteeMember member)
        {
            try
            {
                foreach (var val in member.MemberIdList)
                {
                    var check = db.AssignedMember.Where(e => e.CommitteeMemberID == val && e.EventID == member.EventId).FirstOrDefault();
                    if (check == null)
                    {
                        var assignedEntity = new AssignedMember
                        {
                            EventID = member.EventId,
                            CommitteeMemberID = val,
                            Status = member.Status
                        };

                        db.AssignedMember.Add(assignedEntity);
                        db.SaveChanges();
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, "Member assigned successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + ex.Message);
            }
        }

        //shah
        //
        public class Eventdeleterequest
        {
            public int EventId { get; set; }
        }
        [HttpPost]
        public HttpResponseMessage DeleteEvent(Eventdeleterequest eventid)
        {
            try
            {
                var eve = db.Event.Find(eventid.EventId);
                db.Event.Remove(eve);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "event delete successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }


        [HttpGet]
        public HttpResponseMessage ShowUpcomingEvents()
        {
            try
            {
                var today = DateTime.Now;
                var dates = db.Event.Select(e => e.EventDate);//

                // var data = db.Event.Where(e => (e.EventDate) > today).ToList();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Show events with expired registration dates
        [HttpGet]
        public HttpResponseMessage ShowExpiredEvents()
        {
            try
            {
                var today = DateTime.Now;
                // var data = db.Event.Where(e => e.RegEndDate < today).ToList();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Create a new event
        [HttpPost]
        public HttpResponseMessage CreateEvent()
        {
            try
            {
                var request = HttpContext.Current.Request;
                var path = HttpContext.Current.Server.MapPath("~/Images");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = "";
                for (int i = 0; i < request.Files.Count; i++)
                {
                    fileName = DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss") + request.Files[i].FileName;
                    request.Files[i].SaveAs(Path.Combine(path, fileName));
                }

                var form = request.Form;
                string info = form["submitInfo"];
                var submitdata = JsonConvert.DeserializeObject<Event>(info);
                submitdata.Image = fileName;

                db.Event.Add(submitdata);
                db.SaveChanges();

                // ✅ Return the newly created Event ID instead of just a success message
                return Request.CreateResponse(HttpStatusCode.OK, submitdata);
            }
            catch (Exception )
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("api/Main/CreateTask")]
        public HttpResponseMessage CreateTask()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                int eventId = int.Parse(httpRequest.Form["EventID"]);
                string startTimeStr = httpRequest.Form["TaskStartTime"];
                string endTimeStr = httpRequest.Form["TaskEndTime"];
                string description = httpRequest.Form["Description"];

                if (eventId <= 0 || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(startTimeStr) || string.IsNullOrWhiteSpace(endTimeStr))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "All fields are required.");
                }

                TimeSpan startTime, endTime;
                if (!TimeSpan.TryParse(startTimeStr, out startTime) || !TimeSpan.TryParse(endTimeStr, out endTime))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid time format.");
                }

                // Handle image
                string imageName = null;
                if (httpRequest.Files.Count > 0)
                {
                    var file = httpRequest.Files[0];
                    if (file != null && file.ContentLength > 0)
                    {
                        var uploadsFolder = HttpContext.Current.Server.MapPath("~/Images");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        imageName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, imageName);
                        file.SaveAs(filePath);
                    }
                }

                var newTask = new TalentHunt1.Models.Task
                {
                    EventID = eventId,
                    TaskStartTime = startTimeStr,
                    TaskEndTime = endTimeStr,
                    Description = description,
                    Image = imageName
                };

                db.Task.Add(newTask);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Task created successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }
        [HttpPost]
        
        public HttpResponseMessage UpdateTask()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Get data from form
                int taskId = int.Parse(httpRequest.Form["Id"]);
                int eventId = int.Parse(httpRequest.Form["EventID"]);
                string startTimeStr = httpRequest.Form["TaskStartTime"];
                string endTimeStr = httpRequest.Form["TaskEndTime"];
                string description = httpRequest.Form["Description"];

                if (taskId <= 0 || eventId <= 0 || string.IsNullOrWhiteSpace(description)
                    || string.IsNullOrWhiteSpace(startTimeStr) || string.IsNullOrWhiteSpace(endTimeStr))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "All fields are required.");
                }

                TimeSpan startTime, endTime;
                if (!TimeSpan.TryParse(startTimeStr, out startTime) || !TimeSpan.TryParse(endTimeStr, out endTime))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid time format.");
                }

                // Find task
                var task = db.Task.FirstOrDefault(t => t.Id == taskId);
                if (task == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Task not found.");
                }

                // Update properties
                task.EventID = eventId;
                task.TaskStartTime = startTimeStr;
                task.TaskEndTime = endTimeStr;
                task.Description = description;

                // Handle new image (optional)
                if (httpRequest.Files.Count > 0)
                {
                    var file = httpRequest.Files[0];
                    if (file != null && file.ContentLength > 0)
                    {
                        var uploadsFolder = HttpContext.Current.Server.MapPath("~/Images");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        // Save new image
                        string imageName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, imageName);
                        file.SaveAs(filePath);

                        // Optionally delete old image
                        if (!string.IsNullOrEmpty(task.Image))
                        {
                            var oldImagePath = Path.Combine(uploadsFolder, task.Image);
                            if (File.Exists(oldImagePath))
                                File.Delete(oldImagePath);
                        }

                        // Update image name
                        task.Image = imageName;
                    }
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Task updated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }


        /// <summary>
        /// Get all tasks with optional filtering by event ID
        /// </summary>
        /// <param name="eventId">Optional event ID to filter tasks</param>
        /// <returns>List of tasks with image URLs</returns>
        [HttpGet]
        [Route("api/Main/GetAllTasks")]
        public HttpResponseMessage ShowAllTask(int eventId )
        {
            try
            {
                // Get the base URL for the application
                var request = HttpContext.Current.Request;
                var appUrl = request.Url.GetLeftPart(UriPartial.Authority) + request.ApplicationPath.TrimEnd('/');

                // Query tasks with optional event filter
                var query = db.Task.AsQueryable();

                if (eventId != 0)
                {
                    query = query.Where(t => t.EventID == eventId);
                }

                // Get tasks and transform to include full image URL
                var tasks = query.ToList().Select(t => new
                {
                    t.Id,
                    t.EventID,
                    t.Description,
                    t.TaskStartTime,
                    t.TaskEndTime,
                    // For the Image property, create the full URL if it exists
                    ImageUrl = !string.IsNullOrEmpty(t.Image)
                        ? $"{appUrl}/Images/{t.Image}"
                        : null
                }).ToList();

                // Return success with the list of tasks
                return Request.CreateResponse(HttpStatusCode.OK, tasks);
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error getting tasks: {ex.Message}");

                // Return error response
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { error = "An error occurred while retrieving tasks.", details = ex.Message }
                );
            }
        }
        // Submit a task for an event
        //[HttpPost]
        //public HttpResponseMessage SubmitTask(Submission a)
        //{
        //    try
        //    {
        //        db.Submission.Add(a);
        //        db.SaveChanges();
        //        return Request.CreateResponse(HttpStatusCode.OK, a);
        //    }
        //    catch
        //    {
        //        return Request.CreateResponse(HttpStatusCode.NotFound);
        //    }
        //}

        // Assign members to an event


        // Show assigned members of an event
        //[HttpGet]
        //public HttpResponseMessage ShowAssignedMembers(int eventid)
        //{
        //    try
        //    {
        //        var data = db.AssignedMember.Where(x => x.EventID == eventid).ToList();
        //        return Request.CreateResponse(HttpStatusCode.OK, data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        // Add rules to an event
        //public class Ruletable
        //{
        //    public int Id { get; set; }
        //    public int EventID { get; set; }
        //    public List<string> Rules { get; set; }
        //}

        //[HttpPost]
        //public HttpResponseMessage AddRules(Ruletable val)
        //{
        //    try
        //    {
        //        foreach (var rule in val.Rules)
        //        {
        //            var ruleEntity = new Rules
        //            {
        //                EventID = val.EventID,  // Ensure the EventID is set
        //                Description = rule      // Assign rule description properly
        //            };

        //            db.Rules.Add(ruleEntity);
        //            db.SaveChanges();
        //        }

        //        // Save all rules to the database

        //        return Request.CreateResponse(HttpStatusCode.OK, "Rules added successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        //// Show rules of an event
        //[HttpPost]
        //public HttpResponseMessage ShowRules(Event a)
        //{
        //    try
        //    {
        //        var data = db.Rules.Where(x => x.EventID == a.Id).ToList();
        //        return Request.CreateResponse(HttpStatusCode.OK, data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}
        public class UserRequest
        {
            public int UserId { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage NotificationToAssignMember(UserRequest req)
        {
            try
            {
                if (req == null || req.UserId <= 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data.");

                var cid = db.CommitteeMember
                            .Where(e => e.UserID == req.UserId)
                            .Select(e => e.Id)
                            .FirstOrDefault();

                if (cid == 0)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Committee member not found for this user.");

                var data = (from assigned in db.AssignedMember
                            join ev in db.Event on assigned.EventID equals ev.Id
                            join cm in db.CommitteeMember on assigned.CommitteeMemberID equals cm.Id
                            where assigned.CommitteeMemberID == cid && assigned.Status == "Pending"
                            select new
                            {
                                Id = assigned.Id,
                                EventId = assigned.EventID,
                                Status = assigned.Status,
                                EventTitle = ev.Title,
                                EventImage = ev.Image,
                                MemberName = cm.Name
                            }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception )
            {
                // You should log ex.ToString() somewhere for debugging
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while processing your request.");
            }
        }


        public class RequestStatus
        {
            public int Id { get; set; }
            public string status { get; set; }
        }


        [HttpPost]
        public HttpResponseMessage RequestAcceptReject(RequestStatus req)
        {
            try
            {

                var data = db.AssignedMember.Find(req.Id);
                if (data != null)
                {
                    data.Status = req.status;
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        // Update request status for an assigned member
        //[HttpPost]
        //public HttpResponseMessage UpdateRequestStatus(int eventid, int memberid, string status)
        //{
        //    try
        //    {
        //        var req = db.AssignedMember.Where(a => a.EventID == eventid && a.CommitteeMemberID == memberid).FirstOrDefault();
        //        if (req != null)
        //        {
        //            req.Status = status;
        //            db.SaveChanges();
        //        }
        //        return Request.CreateResponse(HttpStatusCode.OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        // Apply for an event
        [HttpPost]
        public HttpResponseMessage ApplyforEvent(Apply c)
        {
            try
            {
                db.Apply.Add(c);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Request Submit Successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Show all requests for a student
        [HttpGet]
        public HttpResponseMessage ShowAllRequest(int eventid)
        {
            try
            {
                var data = db.Apply.Where(x => x.EventId == eventid).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Update request status for a student
        [HttpPost]
        public HttpResponseMessage UpdateRequestStatusofStudents(int eventid, int studentid, string status)
        {
            try
            {
                var req = db.Apply.Where(a => a.EventId == eventid && a.UserId == studentid).FirstOrDefault();
                if (req != null)
                {
                    req.status = status;

                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, req);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




        // Show upcoming events

        // Show total participants of an event
        [HttpGet]
        public HttpResponseMessage NumberOfParticipants(int eventId)
        {
            try
            {
                var count = db.Apply.Count(a => a.EventId == eventId);
                return Request.CreateResponse(HttpStatusCode.OK, count);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Show the topper of an event
        [HttpGet]
        public HttpResponseMessage ShowEventTopper(int eventId)
        {
            try
            {
                var topper = db.Marks
                    .Where(m => m.SubmissionID == eventId)
                    .OrderByDescending(m => m.Marks1)
                    .FirstOrDefault();
                return Request.CreateResponse(HttpStatusCode.OK, topper);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Search an application by ID

        // Update an application
        [HttpPost]
        public HttpResponseMessage UpdateApply(Apply apply)
        {
            try
            {
                var data = db.Apply.Find(apply.Id);
                if (data != null)
                {
                    data.EventId = apply.EventId;
                    data.UserId = apply.UserId;
                    data.status = apply.status;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Update Event Successfully");
                }
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Delete an application
        [HttpPost]
        public HttpResponseMessage DeleteApply(int id)
        {
            try
            {
                var data = db.Apply.Find(id);
                if (data != null)
                {
                    db.Apply.Remove(data);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Delete Successfully");
                }
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetMarks()
        {
            try
            {
                var result = db.Marks.ToList();
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        // Add marks for a participant
        [HttpPost]
        public HttpResponseMessage AddMarks(Marks marks)
        {
            try
            {
                db.Marks.Add(marks);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        // Search marks by ID


        // Update marks for a participant
        [HttpPost]
        public HttpResponseMessage UpdateMarks(Marks marks)
        {
            try
            {
                var data = db.Marks.Find(marks.Id);
                if (data != null)
                {
                    data.SubmissionID = marks.SubmissionID;
                    data.CommitteeMemberID = marks.CommitteeMemberID;
                    data.Marks1 = marks.Marks1;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Delete marks for a participant
        [HttpPost]
        public HttpResponseMessage DeleteMarks(int id)
        {
            try
            {
                var data = db.Marks.Find(id);
                if (data != null)
                {
                    db.Marks.Remove(data);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Generate a report for an event
        [HttpGet]
        public HttpResponseMessage GenerateEventReport(int eventId)
        {
            try
            {
                var eventReport = (from e in db.Event
                                   where e.Id == eventId
                                   select new
                                   {
                                       e.Title,
                                       e.EventDate,
                                       ParticipantCount = db.Apply.Count(a => a.EventId == e.Id),

                                       Submissions = (
                                           from s in db.Submission
                                           join t in db.Task on s.TaskID equals t.Id
                                           join u in db.Users on s.UserID equals u.Id
                                           join m in db.Marks on s.Id equals m.SubmissionID
                                           where t.EventID == e.Id
                                           group new { s, u, m } by new { u.Id, u.Name } into g
                                           let best = g
                                               .OrderByDescending(x => x.m.Marks1)
                                               .ThenBy(x => x.s.SubmissionTime)
                                               .FirstOrDefault()
                                           orderby best.m.Marks1 descending
                                           select new
                                           {
                                               StudentName = best.u.Name,
                                               best.s.SubmissionTime,
                                               best.s.PathofSubmission,
                                               Marks = best.m.Marks1
                                           }
                                       ).ToList()
                                   }).FirstOrDefault();

                if (eventReport == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Event not found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, eventReport);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




        [HttpGet]
        public HttpResponseMessage SearchApply(int id)
        {
            try
            {
                var data = db.Apply.Find(id);
                return data != null
                    ? Request.CreateResponse(HttpStatusCode.OK, data)
                    : Request.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage ShowAllSubmission()
        {
            try
            {
                var data = db.Submission.ToList();
                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        
        public HttpResponseMessage GetEventById(int eventid)
        {
            using (var db = new Talent_HuntEntities5())
            {
                var ev = db.Event.FirstOrDefault(e => e.Id == eventid);

                if (ev == null)
                {
                    return Request.CreateResponse("No event found"); // 404
                }

                return Request.CreateResponse(ev);// 200 with event data
            }
        }


        [HttpPost]
        public HttpResponseMessage SubmitTask()
        {
            try
            {
                var request = HttpContext.Current.Request;
                var server = HttpContext.Current.Server;
                var uploadPath = server.MapPath("~/Images");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var form = request.Form;

                if (form["submitInfo"] == null)
                    return Request.CreateResponse( "Missing form data: 'submitInfo'.");

                // Deserialize JSON into Submission object
                var submission = JsonConvert.DeserializeObject<Submission>(form["submitInfo"]);
                if (submission == null)
                    return Request.CreateResponse( "Invalid submission data.");

                // Validate task
                var task = db.Task.FirstOrDefault(t => t.Id == submission.TaskID);
                if (task == null)
                    return Request.CreateResponse( "Task not found.");

                // Get associated event using EventID from Task
                var eventInfo = db.Event.FirstOrDefault(e => e.Id == task.EventID);
               

                DateTime today = DateTime.Now.Date;
                DateTime eventDate = Convert.ToDateTime(eventInfo.EventDate).Date;

                if (today != eventDate)
                    return Request.CreateResponse( "Submissions are only allowed on the event date.");

                // Check if submission already exists
                bool alreadySubmitted = db.Submission.Any(s => s.TaskID == submission.TaskID && s.UserID == submission.UserID);
                if (alreadySubmitted)
                    return Request.CreateResponse( "You have already submitted this task.");

                // Check submission time against task timing
                DateTime now = DateTime.Now;
                DateTime taskStart = Convert.ToDateTime(task.TaskStartTime);
                DateTime taskEnd = Convert.ToDateTime(task.TaskEndTime);

                if (now < taskStart)
                    return Request.CreateResponse( "Task has not started yet.");

                if (now > taskEnd)
                    return Request.CreateResponse( "Task submission time is over.");

                // Handle file upload
                if (request.Files.Count == 0)
                    return Request.CreateResponse( "No file uploaded.");

                var uploadedFile = request.Files[0];
                if (uploadedFile == null || uploadedFile.ContentLength == 0)
                    return Request.CreateResponse( "Uploaded file is empty.");

                // Save the uploaded file
                string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff") + "_" + Path.GetFileName(uploadedFile.FileName);
                string filePath = Path.Combine(uploadPath, fileName);
                uploadedFile.SaveAs(filePath);

                // Set submission data
                submission.PathofSubmission = fileName;
                submission.SubmissionTime = now.ToString("yyyy-MM-dd HH:mm:ss");

                // Save to DB
                db.Submission.Add(submission);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Submission uploaded successfully.", id = submission.Id });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while processing the submission: " + ex.Message);
            }
        }



        [HttpGet]
        public HttpResponseMessage GetAssignedEventsForMember(int userId)
        {
            try
            {
                using (var db = new Talent_HuntEntities5())
                {
                    // Step 1: Find CommitteeMemberID for the given userId
                    var committeeMember = db.CommitteeMember.FirstOrDefault(cm => cm.UserID == userId);
                    if (committeeMember == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Committee member not found for the given userId.");
                    }

                    int committeeMemberId = committeeMember.Id;

                    // Step 2: Fetch assigned events for this committee member
                    var assignedEvents = (from am in db.AssignedMember
                                          join ev in db.Event on am.EventID equals ev.Id
                                          where am.CommitteeMemberID == committeeMemberId && am.Status == "Accepted"
                                          select new
                                          {
                                              AssignedMemberId = am.Id,
                                              am.EventID,
                                              am.CommitteeMemberID,
                                              am.Status,
                                              EventTitle = ev.Title,
                                              EventImage = ev.Image
                                          }).ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, assignedEvents);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



        [HttpGet]
        public HttpResponseMessage GetEventsWithTasks()
        {
            try
            {
                var result = from eventDetail in db.Event
                             join task in db.Task on eventDetail.Id equals task.EventID
                             select new
                             {
                                 EventTitle = eventDetail.Title,
                                 TaskDescription = task.Description,
                                 //TaskDate = task.TaskDate,
                                 TaskStartTime = task.TaskStartTime,
                                 TaskEndTime = task.TaskEndTime
                             };

                return Request.CreateResponse(HttpStatusCode.OK, result.ToList());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

        }
        //public HttpResponseMessage GetMarksbyname(string name)
        //{
        //    try
        //    {
        //        var result = from Student in db.Student
        //                     where Student.Name == name
        //                     join submission in db.Submission on Student.Id equals submission.StudentID
        //                     join Task in db.Task on submission.TaskID equals Task.Id
        //                     join marks in db.Marks on submission.Id equals marks.SubmissionID
        //                     join committee in db.CommitteeMember on marks.CommitteeMemberID equals committee.UserID
        //                     join Event in db.Event on Task.EventID equals Event.Id

        //                     select new
        //                     {
        //                         EventTitle = Event.Title,
        //                         StudentName = Student.Name,
        //                         CommiteeMemberName = committee.Name,
        //                         SubmissionPath = submission.PathofSubmission,
        //                         //Taskdate = Task.TaskDate,
        //                         mark = marks.Marks1

        //                     };
        //        return Request.CreateResponse(HttpStatusCode.OK, result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);

        //    }
        //}
        [HttpGet]
        public HttpResponseMessage GetAssignedMembersToEvent(int eventid)
        {
            try
            {
                var result = from eventDetail in db.Event
                             where eventDetail.Id == eventid
                             join assignedMember in db.AssignedMember on
                                eventDetail.Id equals assignedMember.EventID

                             // join committeeMember in db.Users on assignedMember.CommitteeMemberID equals committeeMember.Id
                             select new
                             {

                                 EventTitle = eventDetail.Title,
                                 AssignedMemberID = assignedMember.CommitteeMemberID,
                                 Status = assignedMember.Status
                             };

                return Request.CreateResponse(HttpStatusCode.OK, result.ToList());

            }
            catch (Exception )
            {
                return null;
            }
        }
        public class userAddCommittee
        {

            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public int UserId { get; set; }
            public string Gender { get; set; }
            public string Role = "Committee";
            public string Image { get; set; }
        }

        [HttpPost]
        public HttpResponseMessage AddCommitteeMember()
        {
            try
            {
                var request = HttpContext.Current.Request;
                var path = HttpContext.Current.Server.MapPath("~/Images");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = "";
                for (int i = 0; i < request.Files.Count; i++)
                {
                    fileName = DateTime.Now.ToString("yyyy_MM_dd HH_mm_ss") + request.Files[i].FileName;
                    request.Files[i].SaveAs(Path.Combine(path, fileName));
                }

                var form = request.Form;
                string info = form["submitInfo"];
                Console.WriteLine(info);
                Console.ReadLine();
                var submitdata = JsonConvert.DeserializeObject<userAddCommittee>(info);

                // Check if email already exists in CommitteeMember table
                var existingMember = db.Users.FirstOrDefault(cm => cm.Email == submitdata.Email);
                if (existingMember != null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Email already exists in the Committee Member table.");
                }

                // Validate gender input
                if (submitdata.Gender != "Male" && submitdata.Gender != "Female")
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Gender must be either 'Male' or 'Female'.");
                }

                submitdata.Image = fileName;

                var newuser = new Users
                {
                    // Id = submitdata.Id,  // Uncomment if necessary
                    Name = submitdata.Name,
                    Email = submitdata.Email,
                    Password = submitdata.Password,
                    Role = submitdata.Role // or whatever role you want to assign
                };

                db.Users.Add(newuser);
                db.SaveChanges(); // Save now to generate User ID

                // Add Committee Member
                var newcommitteeMember = new CommitteeMember
                {
                    Name = submitdata.Name,
                    Gender = submitdata.Gender,
                    Image = submitdata.Image,
                    UserID = newuser.Id // if CommitteeMember is linked to User
                };
                db.CommitteeMember.Add(newcommitteeMember);

                db.SaveChanges();

                // Return the newly created Committee Member data
                return Request.CreateResponse(HttpStatusCode.OK, submitdata);
            }
            catch (Exception )
            {
                // Optionally log the exception message (ex.Message)
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while adding the committee member.");
            }
        }




        [HttpPost]
        public HttpResponseMessage UpdateEvent()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                string info = httpRequest.Form["submitInfo"];

                if (string.IsNullOrWhiteSpace(info))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Event data is missing.");
                }

                // Deserialize event JSON data
                Event updatedEvent;
                try
                {
                    updatedEvent = JsonConvert.DeserializeObject<Event>(info);
                }
                catch (Exception)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid JSON format.");
                }

                if (updatedEvent == null || updatedEvent.Id <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid event details.");
                }

                // Find the existing event
                var existingEvent = db.Event.FirstOrDefault(e => e.Id == updatedEvent.Id);
               

                // Update event fields
                existingEvent.Title = updatedEvent.Title;
                existingEvent.Details = updatedEvent.Details;
              //  existingEvent.Venue = updatedEvent.Venue;
                existingEvent.RegStartDate = updatedEvent.RegStartDate;
                existingEvent.RegEndDate = updatedEvent.RegEndDate;
                existingEvent.EventEndTime = updatedEvent.EventEndTime;
                existingEvent.EventStartTime = updatedEvent.EventStartTime;
                existingEvent.EventDate = updatedEvent.EventDate;

                // Handle uploaded image (optional)
                var uploadedFile = httpRequest.Files["file"];
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                   // var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" ,".pdf"};
                   

                    var fileName = Path.GetFileName(uploadedFile.FileName);
                    var path = HttpContext.Current.Server.MapPath("~/Images");

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    var fullPath = Path.Combine(path, fileName);
                    uploadedFile.SaveAs(fullPath);

                    existingEvent.Image = fileName;
                }

                // Save changes
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Event updated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }


        [HttpGet]
        [Route("api/Main/GetAllCommitteeMember")]
        public HttpResponseMessage GetAllCommitteeMember()
        {
            try
            {
                // Join CommitteeMember with Users to get full info
                var members = (from cm in db.CommitteeMember
                               join u in db.Users on cm.UserID equals u.Id
                               where u.Role == "Committee"
                               select new
                               {
                                   cm.Id,
                                   Name = u.Name,
                                   u.Email,
                                   cm.Gender,
                                   cm.Image,
                                   cm.UserID
                               }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error fetching committee members: " + ex.Message);
            }
        }
        // POST: api/Main/DeleteCommitteeMember/5
        public class deletecom
        {
            public int id { get; set; }
        }
        [HttpPost]
        public HttpResponseMessage DeleteCommitteeMember(deletecom id)
        {
            try
            {
                var com = db.CommitteeMember.Find(id.id);
                db.CommitteeMember.Remove(com);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Committee Member delete successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }
        [HttpGet]
        [Route("api/Main/ShowAssignedMembers")]
        public IHttpActionResult ShowAssignedMembers(int eventid)
        {
            try
            {
                var assignedMembers = (from a in db.AssignedMember
                                       join u in db.CommitteeMember on a.CommitteeMemberID equals u.Id
                                       where a.EventID == eventid
                                       select new
                                       {
                                           MemberId = u.Id,
                                           MemberName = u.Name,
                                           Status = a.Status,
                                           Image = u.Image
                                       }).ToList();

                return Ok(assignedMembers);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [Route("Apply")]
        [HttpPost]
        [Route("api/Main/Apply")]
     
        public HttpResponseMessage Apply()
        {
            var httpContext = HttpContext.Current;
            var request = httpContext.Request;

            int userId = Convert.ToInt32(request.Form["UserId"]);
            int eventId = Convert.ToInt32(request.Form["EventId"]);
           

            try
            {
                var student = db.Users.FirstOrDefault(u => u.Id == userId);
                var eventInfo = db.Event.FirstOrDefault(e => e.Id == eventId);

                if (student == null || eventInfo == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "User or Event not found.");
                }

                var alreadyApplied = db.Apply.Any(a => a.UserId == userId && a.EventId == eventId);
                if (alreadyApplied)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "User has already applied for this event.");
                }

                DateTime regStart = DateTime.Parse(eventInfo.RegStartDate);
                DateTime regEnd = DateTime.Parse(eventInfo.RegEndDate);
                if (regEnd < DateTime.Now || regStart > DateTime.Now)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Registration for this event is not currently open.");
                }

                var applyEntry = new Apply
                {
                    UserId = userId,
                    EventId = eventId,
                    status = "Pending",
                   
                };

                db.Apply.Add(applyEntry);
                db.SaveChanges();

                var result = new
                {
                    StudentName = student.Name,
                    EventTitle = eventInfo.Title,
                    Status = "Pending",
                  
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }
        [HttpGet]
       
        public HttpResponseMessage ViewApplications(int eventId)
        {
            try
            {
                var eventInfo = db.Event.FirstOrDefault(e => e.Id == eventId);
                if (eventInfo == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Event not found.");
                }

                // Fetch applications with user name and event title
                var applications = (from a in db.Apply
                                    join u in db.Users on a.UserId equals u.Id
                                    join e in db.Event on a.EventId equals e.Id
                                    where a.EventId == eventId
                                    select new
                                    {
                                        ApplicationId = a.Id,
                                        UserId = a.UserId,
                                        StudentName = u.Name,
                                        EventId = a.EventId,
                                        EventTitle = e.Title,
                                        Status = a.status
                                    }).ToList();

                if (!applications.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No applications found for this event.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, applications);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpPost]
     
        public HttpResponseMessage AddMarks(int SubmissionID, int CommitteeMemberID, int Marks)
        {
            try
            {
                var mark = new Marks
                {
                    SubmissionID = SubmissionID,
                    CommitteeMemberID = CommitteeMemberID,
                    Marks1 = Marks
                };

                db.Marks.Add(mark);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Created, "Marks added successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + ex.Message);
            }
        }


        [HttpPost]
        [Route("api/Main/UpdateApplicationStatus")]
        public HttpResponseMessage UpdateApplicationStatus()
        {
            var httpContext = HttpContext.Current;
            var request = httpContext.Request;

            try
            {
                int applyId = Convert.ToInt32(request.Form["applyId"]);
                string newStatus = request.Form["newStatus"];

                var application = db.Apply.FirstOrDefault(a => a.Id == applyId);
                if (application == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Application not found.");
                }

                application.status = newStatus;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Message = $"Application #{applyId} status updated to '{newStatus}'."
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpGet]
        [Route("api/Main/RegisteredEvents")]
        public HttpResponseMessage RegisteredEvents(int UserId)
        {
            try
            {
                              var registeredEvents = (from u in db.Event
                                        join a in db.Apply  on u.Id  equals a.EventId
                                        where a.UserId == UserId 
                                        select new
                                       {
                                            eventid=u.Id,
                                           Image = u.Image,
                                           Name = u.Title,
                                          
                                           Status = a.status
                                           
                                       }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, registeredEvents);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage LeaderBoard()
        {
            try
            {
                var result = db.Event
                    .Select(e => new
                    {
                        EventId = e.Id,
                        EventTitle = e.Title,

                        TopThreeToppers = (
                            from s in db.Submission
                            join t in db.Task on s.TaskID equals t.Id
                            join u in db.Users on s.UserID equals u.Id
                            join m in db.Marks on s.Id equals m.SubmissionID
                            where t.EventID == e.Id
                            group new { s, u, m } by new { u.Id, u.Name } into g
                            let best = g.OrderByDescending(x => x.m.Marks1).ThenBy(x => x.s.SubmissionTime).FirstOrDefault()
                            orderby best.m.Marks1 descending, best.s.SubmissionTime ascending
                            select new
                            {
                                StudentName = best.u.Name,
                                Marks = best.m.Marks1,
                                SubmissionTime = best.s.SubmissionTime
                            }
                        ).Take(3).ToList()
                    }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }











    }
}
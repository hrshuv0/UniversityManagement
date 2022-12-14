using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using UniversityManagement.Data;
using UniversityManagement.Models;
using UniversityManagement.Models.ViewModels;

namespace UniversityManagement.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<InstructorsController> _logger;

        public InstructorsController(SchoolContext context, ILogger<InstructorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Instructors
        public async Task<IActionResult> Index(int? id, int? courseId)
        {
            var instructorsVm = new InstructorIndexData();

            instructorsVm.Instructors = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments!)
                .ThenInclude(i => i.Course)
                .ThenInclude(i => i.Enrollments!)
                .ThenInclude(i => i.Student!)
                .Include(i => i.CourseAssignments!)
                .ThenInclude(i => i.Course)
                .ThenInclude(i => i.Department)
                .AsNoTracking()
                .OrderBy(i => i.LastName)
                .ToListAsync();

            if(id != null)
            {
                ViewData["InstructorID"] = id.Value;
                Instructor instructor = instructorsVm.Instructors.Single(i => i.ID == id.Value);

                instructorsVm.Courses = instructor.CourseAssignments!.Select(s => s.Course);
            }

            if(courseId is not null)
            {
                ViewData["CourseId"] = courseId.Value;
                instructorsVm.Enrollments = instructorsVm.Courses.Single(x => x.CourseID == courseId).Enrollments!;
            }

              
            return View(instructorsVm);
        }

        // GET: Instructors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(m => m.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        // GET: Instructors/Create
        public IActionResult Create()
        {
            var instructor = new Instructor();
            instructor.CourseAssignments = new List<CourseAssignment>();
            PopulateAssignedCourseData(instructor);

            return View(instructor);
        }

        // POST: Instructors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LastName,FirstMidName,HireDate,OfficeAssignment")] Instructor instructor, string[] selectedCourses)
        {
            if(selectedCourses != null)
            {
                instructor.CourseAssignments = new List<CourseAssignment>();
                foreach(var course in selectedCourses)
                {
                    var courseToAdd = new CourseAssignment() { InstructorID = instructor.ID, CourseID = int.Parse(course) };
                    instructor.CourseAssignments.Add(courseToAdd);
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateAssignedCourseData(instructor);

            _logger.LogInformation("Instructor Created with name {InstructorFullName}", instructor.FullName);
            return View(instructor);
        }

        // GET: Instructors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Instructors == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)!
                .ThenInclude(i => i.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ID == id);
            
            if (instructor == null)
            {
                return NotFound();
            }

            PopulateAssignedCourseData(instructor);

            return View(instructor);
        }

        // POST: Instructors/Edit/5
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id, string[] selectedCourses)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructorToUpdate = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)!
                .ThenInclude(i => i.Course)
                .FirstOrDefaultAsync(s => s.ID == id);

            if (await TryUpdateModelAsync<Instructor>(
                instructorToUpdate,
                "",
                i => i.FirstMidName, i => i.LastName, i => i.HireDate, i => i.OfficeAssignment))
            {
                if (String.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment?.Location))
                {
                    instructorToUpdate.OfficeAssignment = null;
                }

                UpdateInstructorCourses(selectedCourses, instructorToUpdate);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
                return RedirectToAction(nameof(Index));
            }

            UpdateInstructorCourses(selectedCourses, instructorToUpdate);
            PopulateAssignedCourseData(instructorToUpdate);

            return View(instructorToUpdate);
        }

        private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            if(selectedCourses == null)
            {
                instructorToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructorToUpdate.CourseAssignments!.Select(c => c.CourseID));

            foreach(var course in _context.Courses)
            {
                if(selectedCoursesHS.Contains(course.CourseID.ToString()))
                {
                    if(!instructorCourses.Contains(course.CourseID))
                    {
                        instructorToUpdate.CourseAssignments!.Add(new CourseAssignment
                        {
                            InstructorID = instructorToUpdate.ID,
                            CourseID = course.CourseID
                        });
                    }
                }
                else
                {
                    if(instructorCourses.Contains(course.CourseID))
                    {
                        CourseAssignment courseToRemove = instructorToUpdate.CourseAssignments!.FirstOrDefault(c => c.CourseID == course.CourseID)!;
                        _context.Remove(courseToRemove);
                    }
                }
            }
        }

        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var courseList = _context.Courses;
            var instructorCourses = new HashSet<int>(instructor.CourseAssignments!.Select(c => c.CourseID));
            var viewModel = new List<AssignedCourseData>();

            foreach(var course in courseList)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Assigned = instructorCourses.Contains(course.CourseID)
                });
            }
            ViewData["Courses"] = viewModel;
        }

        // GET: Instructors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Instructors == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(m => m.ID == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        // POST: Instructors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Instructors == null)
            {
                return Problem("Entity set 'SchoolContext.Instructors'  is null.");
            }

            var instructor = await _context.Instructors
                .Include(i => i.CourseAssignments)
                .SingleAsync(i => i.ID == id);

            var departmentList = await _context.Departments
                .Where(d => d.InstructorID == id)
                .ToListAsync();
            departmentList.ForEach(d => d.InstructorID = null);


            if (instructor != null)
            {
                _context.Instructors.Remove(instructor);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InstructorExists(int id)
        {
          return (_context.Instructors?.Any(e => e.ID == id)).GetValueOrDefault();
        }
    }
}

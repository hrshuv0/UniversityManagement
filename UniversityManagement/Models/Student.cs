using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;

namespace UniversityManagement.Models
{
    public class Student
    {
        public int ID { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[A-Z]+[a-zA-z]*$")]
        public string LastName { get; set; }

        [StringLength(50)]
        [RegularExpression(@"^[A-Z]+[a-zA-z]*$")]
        public string FirstMidName { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString ="{0:dd-MM-yyyy}", ApplyFormatInEditMode =true)]
        public DateTime EnrollmentDate { get; set; }

        public ICollection<Enrollment>? Enrollments { get; set; }
    }
}

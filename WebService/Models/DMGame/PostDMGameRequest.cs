using System.ComponentModel.DataAnnotations;

namespace BHG.WebService
{
    public class PostDMGameRequest : BaseModel
    {
        [Required]
        public DMGameAction? AcionTypeId { get; set; }

        [Required]
        public string UserName { get; set; }

        public string TargetUserName { get; set; }

        public List<int> TargetCardIds { get; set; }
    }
}

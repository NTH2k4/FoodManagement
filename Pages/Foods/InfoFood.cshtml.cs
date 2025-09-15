using FoodManagement.Contracts.Foods;
using FoodManagement.Models;
using FoodManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Foods
{
    public class InfoFoodModel : PageModel
    {
        private readonly IFoodRepository _foodRepository;

        public InfoFoodModel(IFoodRepository foodRepository)
        {
            _foodRepository = foodRepository;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public FoodDto? Food { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Food = await _foodRepository.GetByIdAsync(id.ToString());
            if (Food == null)
            {
                return Page();
            }
            return Page();
        }
    }
}

using FoodManagement.Contracts;
using FoodManagement.Models;
using FoodManagement.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodManagement.Pages.Foods
{
    public class InfoFoodModel : PageModel
    {
        private readonly IRepository<FoodDto> _foodRepository;

        public InfoFoodModel(IRepository<FoodDto> foodRepository)
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

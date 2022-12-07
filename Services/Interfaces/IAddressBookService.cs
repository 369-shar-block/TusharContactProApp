using TusharContactProApp.Models;

namespace TusharContactProApp.Services.Interfaces
{
    public interface IAddressBookService
    {
        public Task AddContactToCategoryAsync(int categoryId, int contactId);

        public Task AddContactToCategoriesAsync(IEnumerable<int> categoryId, int contactId);
        public Task<bool> IsContactInCategory(int categoryId, int contactId);
        public Task<IEnumerable<Category>> GetAppCategoriesAsync(string appUserId);

        public Task RemoveAllContactCategoriesAsync(int contactId);

    }
}

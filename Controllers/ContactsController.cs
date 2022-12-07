using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TusharContactProApp.Data;
using TusharContactProApp.Enums;
using TusharContactProApp.Models;
using TusharContactProApp.Services.Interfaces;

namespace TusharContactProApp.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;

        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
        }

        // GET: Contacts
        [Authorize]
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User);

            List<Contact> contacts = await _context.Contacts.
                Where(c => c.AppUserId == userId).
                Include(c =>c.Categories).
                Include(c => c.AppUser).
                ToListAsync();

            List<Category> userCategories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();
            ViewData["CategoryId"] = new SelectList(userCategories, "Id", "Name");

            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());

            string userId = _userManager.GetUserId(User);
            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();
            ViewData["CategoryList"] = new MultiSelectList(categories, "Id", "Name");
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, List<int> categoryList)
        {
            ModelState.Remove("AppUserId");
            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User); 
                contact.Created = DateTime.UtcNow;

                if(contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }

                if(contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                //to do
                foreach(int categoryId in categoryList)
                {
                    await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                }



                return RedirectToAction(nameof(Index));
            }

            string userId = _userManager.GetUserId(User);
            List<Category> categories = await _context.Categories.Where(c => c.AppUserId == userId).ToListAsync();
            ViewData["CategoryList"] = new MultiSelectList(categories, "Id", "Name");

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());

            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            

            Contact? contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == appUserId);

            if (contact == null)
            {
                return NotFound();
            }


            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());


            List<Category> categories = (await _addressBookService.GetAppCategoriesAsync(appUserId)).ToList();
            List<int> categoryIds = contact.Categories.Select(c => c.Id).ToList(); 


            ViewData["CategoryList"] = new MultiSelectList(categories, "Id", "Name", categoryIds);
           
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageFile,ImageData,ImageType")] Contact contact, List<int> categoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);

                    if (contact.BirthDate != null)
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }

                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    //remove current categories
                    await _addressBookService.RemoveAllContactCategoriesAsync(contact.Id);

                    //add selected categories
                    await _addressBookService.AddContactToCategoriesAsync(categoryList, contact.Id);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }


            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());


            List<Category> categories = (await _addressBookService.GetAppCategoriesAsync(contact.AppUserId!)).ToList();
            List<int> categoryIds = contact.Categories.Select(c => c.Id).ToList();


            ViewData["CategoryList"] = new MultiSelectList(categories, "Id", "Name", categoryIds);

            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
          return _context.Contacts.Any(e => e.Id == id);
        }
    }
}

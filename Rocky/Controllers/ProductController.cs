using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rocky.Data;
using Rocky.Models;
using Rocky.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)] // only Admin Users can access this Route
    public class ProductController : Controller
    {
        // section 4
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment; // to access the WC.cs class where we store variables (section 4-12) 
        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {


            // deal with the foreign keys

            // bad aproach

            /*
            IEnumerable<Product> objList = _db.Product;

            foreach(var obj in objList)
            {
                obj.Category = _db.Category.FirstOrDefault(u => u.Id == obj.CategoryId);
                obj.ApplicationType = _db.ApplicationType.FirstOrDefault(u => u.Id == obj.ApplicationTypeId);

            }
            */

            // good approach

            IEnumerable<Product> objList = _db.Product.Include(u=>u.Category).Include(u=>u.ApplicationType);

            return View(objList);
        }

        // GET: Upsert  /* upsert is create and edit in the same action */
        public IActionResult Upsert(int? id) // ? is for create/add functionality (because we are not passing id parameter, id is for edit functionality)
        {
            // display all of the categories in a dropdown input type in the View

            /* Right approach with ViewModels */

            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            /*
            // BAD Approach with ViewBag ==> just a temporary data
            IEnumerable<SelectListItem> CategoryDropDown = _db.Category.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
            ViewBag.CategoryDropDown = CategoryDropDown; // pass the categories into the view file 
            Product product = new Product();
            */

            if (id == null)
            {
                // this is for create
                return View(productVM);
            }
            else
            {
                // this is for edit

                productVM.Product = _db.Product.Find(id);
                if(productVM.Product == null)
                {
                    return NotFound();
                }
                return View(productVM);
            }
        }

        // POST: Upsert 
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                // below is written approach to upload the image files FROM local directory TO wwwroot/images/product/
                
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;

                if(productVM.Product.Id == 0)
                {
                    // Creating...
                    string upload = webRootPath + WC.ImagePath; // where we want to store(upload) the images
                    string fileName = Guid.NewGuid().ToString(); // what is the FileName we want to give that will be uploaded in the folder... Guid is random name
                    string extension = Path.GetExtension(files[0].FileName); // get the extension from files

                    // actual upload functionality
                    using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }

                    productVM.Product.Image = fileName + extension;

                    _db.Product.Add(productVM.Product);
                }
                else
                {
                    // Updating...
                    var objFromDb = _db.Product.AsNoTracking().FirstOrDefault(u => u.Id == productVM.Product.Id); // retrieve data from the database. Because EF can track only 1 file at the time, we don't need to track this file from EF, and we use the method AsNoTracking()

                    if (files.Count > 0)
                    {
                        // upload the new file
                        string upload = webRootPath + WC.ImagePath; // where we want to store(upload) the images
                        string fileName = Guid.NewGuid().ToString(); // what is the FileName we want to give that will be uploaded in the folder... Guid is random name
                        string extension = Path.GetExtension(files[0].FileName); // get the extension from files

                        // remove the old file
                        var oldFile = Path.Combine(upload, objFromDb.Image); // get the path from the app

                        if(System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }

                        // add the new file
                        using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                        {
                            files[0].CopyTo(fileStream);
                        }

                        productVM.Product.Image = fileName + extension; 
                    }
                    else
                    {
                        // if the image is not edited, then save/keep the same image
                        productVM.Product.Image = objFromDb.Image;
                    }
                    _db.Product.Update(productVM.Product); 
                }
                _db.SaveChanges(); // without this, the changes in the app will not be saved in the database
                return RedirectToAction("Index");
            }

            productVM.CategorySelectList = _db.Category.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            productVM.ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            return View(productVM);
        }

        

        // GET: DELETE
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            //eager loading (two queries into one execution), the 2 objects are executing from the Index action
            Product product = _db.Product.Include(u => u.Category).Include(u=>u.ApplicationType).FirstOrDefault(u => u.Id == id); // take care of the Category and Application Type (two foreign keys)

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST - DELETE 
        [HttpPost, ActionName("Delete")] // because the action name is DeletePost, with adding ActionName("Delete") we tell the compiler that DeletePost need to execute Delete action (above)
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.Product.Find(id);
            if (obj == null)
            {
                return NotFound();
            }

            string upload = _webHostEnvironment.WebRootPath + WC.ImagePath; 

            var oldFile = Path.Combine(upload, obj.Image); 

            if (System.IO.File.Exists(oldFile))
            {
                System.IO.File.Delete(oldFile);
            }

            _db.Product.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}

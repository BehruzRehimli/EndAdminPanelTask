using HomeworkPustok.Areas.Manage.ViewModels;
using HomeworkPustok.DAL;
using HomeworkPustok.Helpers;
using HomeworkPustok.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HomeworkPustok.Areas.Manage.Controllers
{
    [Area("manage")]
    public class BookController : Controller
    {
        private readonly PustokDbContext _context;

        public IWebHostEnvironment _env { get; }

        public BookController(PustokDbContext context,IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        public IActionResult Index(int page=1)
        {
            var books = _context.Books.Include(x => x.Genre).Include(x => x.Author).Include(x => x.Images.Where(x => x.PhotoNumber == 1));
            return View(PaginationMV<Book>.Create(books,page,4));
        }
        public IActionResult Add()
        {
            ViewBag.Authors=_context.Authors.ToList();
            ViewBag.Genres=_context.Genres.ToList();
            ViewBag.Tags=_context.Tags.ToList();
            return View();
        }
        [HttpPost]
        public IActionResult Add(Book book)
        {
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            if (!ModelState.IsValid)
            {
                return View();
            }
            if (book==null)
            {
                return View("error");
            }
            if (book.MainImage==null)
            {
                ModelState.AddModelError("MainImage", "MainImage is required!");
                return View();
            }
            if (book.SecondImage == null)
            {
                ModelState.AddModelError("SecondImage", "SecondImage is required!");
                return View();
            }
            if (_context.Genres.FirstOrDefault(x=>x.Id==book.GenreId)==null|| _context.Authors.FirstOrDefault(x => x.Id == book.AuthorId) == null)
            {
                return View("error");
            }
            _context.Books.Add(book);
            var mainimg = new BookImage() {
                Image = FileMeneger.UploadFile(_env.WebRootPath,"manage/upload/product",book.MainImage),
                PhotoNumber=1,
                Book=book
            };
            _context.Bookİmages.Add(mainimg);
            var secondimg = new BookImage()
            {
                Image = FileMeneger.UploadFile(_env.WebRootPath, "manage/upload/product", book.SecondImage),
                PhotoNumber=2,
                Book=book
            };
            _context.Bookİmages.Add(secondimg);

            foreach (var item in book.MoreImageis)
            {
                var img = new BookImage()
                {
                    Image = FileMeneger.UploadFile(_env.WebRootPath, "manage/upload/product", item),
                    PhotoNumber = 3,
                    Book = book
                };
                _context.Bookİmages.Add(img);
            }
            foreach (int item in book.TagsIds)
            {
                var tag=_context.Tags.FirstOrDefault(x => x.Id == item);
                if (tag == null) return View("error");
                var booktag = new BookTag()
                {
                    Book = book,
                    Tag = tag
                };
                _context.BookTag.Add(booktag);
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        public IActionResult Update(int id)
        {
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            Book book=_context.Books.Include(x=>x.Images).Include(x=>x.Genre).Include(x=>x.Author).Include(x=>x.BookTag).ThenInclude(x=>x.Tag).FirstOrDefault(x=>x.Id==id);
            if (book==null)
            {
                return View("error");
            }

            return View(book);
        }
        [HttpPost]
        public IActionResult Update(Book book)
        {
            ViewBag.Authors = _context.Authors.ToList();
            ViewBag.Genres = _context.Genres.ToList();
            ViewBag.Tags = _context.Tags.ToList();

            Book exsistBook = _context.Books.Include(x => x.Images).Include(x => x.Genre).Include(x => x.Author).Include(x => x.BookTag).ThenInclude(x => x.Tag).FirstOrDefault(x => x.Id == book.Id);
            var removedIamge=new List<string>();
            if (exsistBook==null)
            {
                return View("error");
            }
            if (!_context.Authors.Any(x=>x.Id==book.AuthorId))
            {
                return View("error");
            }
            if (!_context.Genres.Any(x => x.Id == book.GenreId))
            {
                return View("error");
            }
            if (!ModelState.IsValid)
            {
                return View(exsistBook);
            }
            foreach(var item in book.TagsIds)
            {
                if (!_context.Tags.Any(x=>x.Id==item))
                {
                    return View("error");
                }
            }
            if (book.MainImage!=null)
            {
                var oldimg = exsistBook.Images.FirstOrDefault(x => x.PhotoNumber == 1);
                removedIamge.Add(oldimg.Image);
                oldimg.Image = FileMeneger.UploadFile(_env.WebRootPath, "manage/upload/product", book.MainImage);
            }

            if (book.SecondImage != null)
            {
                var oldimg = exsistBook.Images.FirstOrDefault(x => x.PhotoNumber == 2);
                removedIamge.Add(oldimg.Image);
                oldimg.Image = FileMeneger.UploadFile(_env.WebRootPath, "manage/upload/product", book.SecondImage);
            }
            if (book.TagsIds != null)
            {
                var exsistlist = exsistBook.BookTag.Select(x => x.TagId).ToList();
                foreach (var item in book.TagsIds)
                {
                    if (!exsistlist.Contains(item))
                    {
                        var newrel = new BookTag() { TagId = item };
                        exsistBook.BookTag.Add(newrel);
                    }
                }
                foreach (var item in exsistlist)
                {
                    if (!book.TagsIds.Contains(item))
                    {
                        exsistBook.BookTag.Remove(exsistBook.BookTag.FirstOrDefault(x => x.TagId == item));
                    }
                }
            }

            foreach (BookImage item in exsistBook.Images.FindAll(x=>x.PhotoNumber>2).ToList())
            {
                if (!book.MoreImgIds.Contains(item.Id))
                {
                    removedIamge.Add(item.Image);
                    exsistBook.Images.Remove(item);
                }

            }
            foreach (IFormFile item in book.MoreImageis)
            {
                var newimg = new BookImage()
                {
                    Image = FileMeneger.UploadFile(_env.WebRootPath, "manage/upload/product", item),
                    PhotoNumber = 4,
                    Book = exsistBook
                };
                _context.Bookİmages.Add(newimg);
            }

            exsistBook.Title= book.Title;
            exsistBook.AuthorId = book.AuthorId;
            exsistBook.GenreId= book.GenreId;
            exsistBook.Desc= book.Desc;
            exsistBook.Price= book.Price;
            exsistBook.Discount= book.Discount;
            exsistBook.RewardPoint= book.RewardPoint;
            exsistBook.IsFeature= book.IsFeature;
            exsistBook.Availability= book.Availability;


            _context.SaveChanges();
            FileMeneger.DeleteFiles(_env.WebRootPath, "manage/upload/product", removedIamge);
            return RedirectToAction("index");
        }
        public IActionResult Delete(int id)
        {
            if (!_context.Books.Any(x=>x.Id==id))
            {
                return View("error");
            }
            var removeddata=_context.Books.FirstOrDefault(x=>x.Id==id);
            List<BookImage> imgs = removeddata.Images.ToList();
            var rmvimgs = new List<string>();
            foreach (var im in imgs)
            {
                rmvimgs.Add(im.Image);
            }

            _context.Books.Remove(removeddata);
            _context.SaveChanges();
            FileMeneger.DeleteFiles(_env.WebRootPath,"manage/upload/product",rmvimgs);
            return RedirectToAction("index");
        }
    }
}

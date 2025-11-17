using Library_Project.Model;
using Library_Project.Services;
using Library_Project.Services.Interfaces;
using Moq;

namespace Library_Project_Tests
{
    public class LibraryServiceTests
    {
        private readonly Mock<IBookRepository> _repoMock;
        private readonly Mock<IMemberService> _memberMock;
        private readonly Mock<INotificationService> _notifMock;
        private readonly LibraryService _service;

        public LibraryServiceTests()
        {
            _repoMock = new Mock<IBookRepository>();
            _memberMock = new Mock<IMemberService>();
            _notifMock = new Mock<INotificationService>();

            _service = new LibraryService(_repoMock.Object, _memberMock.Object, _notifMock.Object);
        }

        /// <summary>
        /// Verify: Should add a new book when it does not exist.
        /// </summary>
        [Fact]
        public void AddBook_ShouldAddNewBook_WhenNotExists()
        {
            _repoMock.Setup(r => r.FindBook("1984")).Returns((Book)null);

            _service.AddBook("1984", 3);

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Title == "1984" && b.Copies == 3)), Times.Once);
        }

        /// <summary>
        /// Assert.Equal: Should increase copies when book exists.
        /// </summary>
        [Fact]
        public void AddBook_ShouldIncreaseCopies_WhenBookExists()
        {
            var existing = new Book { Title = "1984", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("1984")).Returns(existing);

            _service.AddBook("1984", 3);

            Assert.Equal(5, existing.Copies);
            _repoMock.Verify(r => r.SaveBook(existing), Times.Once);
        }

        /// <summary>
        /// [Theory][InlineData] & Assert.Throws: Should throw exception for invalid input.
        /// </summary>
        [Theory]
        [InlineData("", 2)]
        [InlineData("Book", 0)]
        public void AddBook_ShouldThrow_WhenInvalidInput(string title, int copies)
        {
            Assert.ThrowsAny<ArgumentException>(() => _service.AddBook(title, copies));
        }

        /// <summary>
        /// Assert.True, Assert.Equal, Verify: Should decrease copies and notify when borrowing is successful.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldDecreaseCopies_WhenValidMemberAndAvailable()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "Dune");

            Assert.True(result);
            Assert.Equal(1, book.Copies);
            _notifMock.Verify(n => n.NotifyBorrow(1, "Dune"), Times.Once);
        }

        /// <summary>
        /// Assert.False, Verify(Times.Never): Should return false and not notify when no copies are available.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldReturnFalse_WhenNoCopies()
        {
            var book = new Book { Title = "Dune", Copies = 0 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "Dune");

            Assert.False(result);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Assert.Throws: Should throw exception when member is invalid.
        /// </summary>
        [Fact]
        public void BorrowBook_ShouldThrow_WhenInvalidMember()
        {
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(false);

            Assert.Throws<InvalidOperationException>(() => _service.BorrowBook(1, "Dune"));
        }

        /// <summary>
        /// Assert.True, Assert.Equal, Verify: Should increase copies and notify when returning a book.
        /// </summary>
        [Fact]
        public void ReturnBook_ShouldIncreaseCopies()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);

            bool result = _service.ReturnBook(1, "Dune");

            Assert.True(result);
            Assert.Equal(2, book.Copies);
            _notifMock.Verify(n => n.NotifyReturn(1, "Dune"), Times.Once);
        }

        /// <summary>
        /// Assert.False: Should return false when book is not found.
        /// </summary>
        [Fact]
        public void ReturnBook_ShouldReturnFalse_WhenBookNotFound()
        {
            _repoMock.Setup(r => r.FindBook("Unknown")).Returns((Book)null);

            bool result = _service.ReturnBook(1, "Unknown");

            Assert.False(result);
        }

        /// <summary>
        /// Assert.NotEmpty, Assert.Contains, Assert.Equal: Should return only books with available copies.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnOnlyBooksWithCopies()
        {
            var all = new List<Book>
            {
                new Book { Title = "A", Copies = 0 },
                new Book { Title = "B", Copies = 1 },
                new Book { Title = "C", Copies = 3 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(all);

            var available = _service.GetAvailableBooks();

            Assert.NotEmpty(available);
            Assert.Contains(available, b => b.Title == "B");
            Assert.Equal(2, available.Count);
        }

        /// <summary>
        /// Assert.Empty: Should return empty list when no books are available.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldReturnEmpty_WhenNoBooksAvailable()
        {
            var all = new List<Book> { new Book { Title = "A", Copies = 0 } };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(all);

            var result = _service.GetAvailableBooks();

            Assert.Empty(result);
        }

        /// <summary>
        /// Verify(Times.AtLeastOnce): Should verify FindBook called at least once.
        /// </summary>
        [Fact]
        public void Verify_MethodsCalled_AtLeastOnce()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            _service.BorrowBook(1, "Dune");

            _repoMock.Verify(r => r.FindBook("Dune"), Times.AtLeastOnce);
        }

        /// <summary>
        /// It.Is(predicate): Should match predicate for book title.
        /// </summary>
        [Fact]
        public void It_Is_PredicateExample()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook(It.Is<string>(s => s.StartsWith("D")))).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            var result = _service.BorrowBook(1, "Dune");

            Assert.True(result);
        }

        /// <summary>
        /// It.IsAny: Should match any title for FindBook and NotifyBorrow.
        /// </summary>
        [Fact]
        public void It_IsAny_ShouldMatchAnyTitle()
        {
            var book = new Book { Title = "Anything", Copies = 2 };
            _repoMock.Setup(r => r.FindBook(It.IsAny<string>())).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            bool result = _service.BorrowBook(1, "RandomTitle");

            Assert.True(result);
            _notifMock.Verify(n => n.NotifyBorrow(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Assert.NotNull: Should return a Book object when searching for an existing book.
        /// </summary>
        [Fact]
        public void FindBook_ShouldReturnBook_WhenExists()
        {
            var book = new Book { Title = "TestBook", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("TestBook")).Returns(book);

            var result = _repoMock.Object.FindBook("TestBook");

            Assert.NotNull(result);
        }

        /// <summary>
        /// Assert.Null: Should return null when searching for a non-existent book.
        /// </summary>
        [Fact]
        public void FindBook_ShouldReturnNull_WhenNotExists()
        {
            _repoMock.Setup(r => r.FindBook("MissingBook")).Returns((Book)null);

            var result = _repoMock.Object.FindBook("MissingBook");

            Assert.Null(result);
        }

        /// <summary>
        /// Assert.NotEqual: Should not return the same number of copies after borrowing.
        /// </summary>
        [Fact]
        public void BorrowBook_CopiesShouldNotEqualOriginal_AfterBorrow()
        {
            var book = new Book { Title = "Dune", Copies = 2 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);
            _memberMock.Setup(m => m.IsValidMember(1)).Returns(true);

            _service.BorrowBook(1, "Dune");

            Assert.NotEqual(2, book.Copies);
        }

        /// <summary>
        /// Verify(Times.Exactly): SaveBook should be called exactly once when returning a book.
        /// </summary>
        [Fact]
        public void ReturnBook_SaveBookCalledExactlyOnce()
        {
            var book = new Book { Title = "Dune", Copies = 1 };
            _repoMock.Setup(r => r.FindBook("Dune")).Returns(book);

            _service.ReturnBook(1, "Dune");

            _repoMock.Verify(r => r.SaveBook(book), Times.Exactly(1));
        }

        /// <summary>
        /// Verify(Times.Never): NotifyReturn should not be called if book does not exist.
        /// </summary>
        [Fact]
        public void ReturnBook_NotifyReturnNeverCalled_WhenBookNotFound()
        {
            _repoMock.Setup(r => r.FindBook("Unknown")).Returns((Book)null);

            _service.ReturnBook(1, "Unknown");

            _notifMock.Verify(n => n.NotifyReturn(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Assert.Contains: GetAvailableBooks should contain a book with positive copies.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldContainBookWithCopies()
        {
            var books = new List<Book>
            {
                new Book { Title = "Book1", Copies = 0 },
                new Book { Title = "Book2", Copies = 2 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(books);

            var result = _service.GetAvailableBooks();

            Assert.Contains(result, b => b.Title == "Book2" && b.Copies == 2);
        }

        /// <summary>
        /// Assert.NotEmpty: GetAvailableBooks should not be empty if there are books with copies.
        /// </summary>
        [Fact]
        public void GetAvailableBooks_ShouldNotBeEmpty_IfBooksAvailable()
        {
            var books = new List<Book>
            {
                new Book { Title = "Book1", Copies = 1 }
            };
            _repoMock.Setup(r => r.GetAllBooks()).Returns(books);

            var result = _service.GetAvailableBooks();

            Assert.NotEmpty(result);
        }

        /// <summary>
        /// It.IsAny: SaveBook should accept any Book object when adding a new book.
        /// </summary>
        [Fact]
        public void AddBook_SaveBookAcceptsAnyBook()
        {
            _repoMock.Setup(r => r.FindBook("NewBook")).Returns((Book)null);

            _service.AddBook("NewBook", 1);

            _repoMock.Verify(r => r.SaveBook(It.IsAny<Book>()), Times.Once);
        }

        /// <summary>
        /// It.Is: SaveBook should be called with a Book having Copies greater than zero.
        /// </summary>
        [Fact]
        public void AddBook_SaveBookWithCopiesGreaterThanZero()
        {
            _repoMock.Setup(r => r.FindBook("PositiveBook")).Returns((Book)null);

            _service.AddBook("PositiveBook", 5);

            _repoMock.Verify(r => r.SaveBook(It.Is<Book>(b => b.Copies > 0)), Times.Once);
        }

    }
}

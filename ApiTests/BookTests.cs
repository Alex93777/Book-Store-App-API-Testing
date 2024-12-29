using Newtonsoft.Json.Linq;
using RestSharp;
using System.Diagnostics;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class BookTests : IDisposable
    {
        private RestClient client;
        private string token;
        private Random random;
        private string title;

        [SetUp]
        public void Setup()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("john.doe@example.com", "password123");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
            random = new Random();
        }

        [Test, Order(1)]
        public void Test_GetAllBooks()
        {
            //Arrange
            var request = new RestRequest("/book", Method.Get);

            //Act
            var response = client.Execute(request);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Response does not have correct status code");

                Assert.That(response.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var books = JArray.Parse(response.Content);

                Assert.That(books.Type, Is.EqualTo(JTokenType.Array),
                    "The response content is not array");

                Assert.That(books.Count, Is.GreaterThan(0),
                    "Books count is below 1");

                foreach(var book in books)
                {
                    Assert.That(book["title"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Title is not as expected");

                    Assert.That(book["author"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Author is not as expected");

                    Assert.That(book["description"]?.ToString(), Is.Not.Null.Or.Empty,
                        "Description is not as expected");

                    Assert.That(book["price"], Is.Not.Null.Or.Empty,
                        "Price is not as expected");

                    Assert.That(book["pages"], Is.Not.Null.Or.Empty,
                        "Pages is not as expected");

                    Assert.That(book["category"], Is.Not.Null.Or.Empty,
                        "Category is not as expected");
                }
            });
        }

        [Test, Order(2)]
        public void Test_GetBookByTitle()
        {
            //Arrange
            //Get request for all books
            var expectedAuthor = "F. Scott Fitzgerald";
            var titleToGet = "The Great Gatsby";
            var getRequest = new RestRequest("/book", Method.Get);

            //Act
            var response = client.Execute(getRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo
                    (HttpStatusCode.OK), "Response code is not correct");

                Assert.That(response.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var books = JArray.Parse(response.Content);
                var book = books.FirstOrDefault(b => b["title"]?.ToString() == titleToGet);

                Assert.That(book, Is.Not.Null, $"Book with title {titleToGet} does not exist");

                Assert.That(book["author"]?.ToString(), Is.EqualTo(expectedAuthor),
                    "Author is not as expected");
            });
        }

        [Test, Order(3)]
        public void Test_AddBook()
        {
            //Arrange
            //Get All categories
            var getAllCategories = new RestRequest("/category", Method.Get);

            var getAllCategoriesResponse = client.Execute(getAllCategories);

            Assert.Multiple(() =>
            {
                Assert.That(getAllCategoriesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Get categories status code is not as expected");

                Assert.That(getAllCategoriesResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");
            });

            var categories = JArray.Parse(getAllCategoriesResponse.Content);

            //Extract the first category id
            var categoryId = categories.First()["_id"]?.ToString();

            //Create request for creating book
            //Arrange
            var createBookRequest = new RestRequest("/book", Method.Post);
            createBookRequest.AddHeader("Authorization", $"Bearer {token}");
            title = $"bookTitle_{random.Next(999, 9999)}";
            var author = "Test author";
            var description = "Test description";
            var price = 20.99;
            var pages = 50;

            createBookRequest.AddBody(new 
            { 
                title = title,
                author,
                description,
                price,
                pages,
                category = categoryId
            });

            //Act
            var addResponse = client.Execute(createBookRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code is not as expected");

                Assert.That(addResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");
            });

            var book = JObject.Parse(addResponse.Content);
            var bookId = book["_id"]?.ToString();

            //Get request for getting by id
            var getByIdRequest = new RestRequest($"/book/{bookId}", Method.Get);

            var getByIdResponse = client.Execute(getByIdRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code is not as expected");

                Assert.That(getByIdResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var createBook = JObject.Parse(getByIdResponse.Content);

                Assert.That(createBook["title"]?.ToString(), Is.EqualTo(title),
                    "New book title is not as expected");

                Assert.That(createBook["author"]?.ToString(), Is.EqualTo(author),
                    "New book author is not as expected");

                Assert.That(createBook["description"]?.ToString(), Is.EqualTo(description),
                    "New book description is not as expected");

                Assert.That(createBook["price"]?.Value<double>(), Is.EqualTo(price),
                    "New book price is not as expected");

                Assert.That(createBook["pages"]?.Value<int>(), Is.EqualTo(pages),
                    "New book pages is not as expected");

                Assert.That(createBook["category"]?["_id"]?.ToString(), Is.EqualTo(categoryId),
                    "Category id is not as expected");
            });

        }

        [Test, Order(4)]
        public void Test_UpdateBook()
        {
            //Arrange
            //Get by title
            var getRequest = new RestRequest("/book", Method.Get);

            //Act
            var getResponse = client.Execute(getRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo
                    (HttpStatusCode.OK), "Response code is not correct");

                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                  "Response content is not as expected");
            });

            var books = JArray.Parse(getResponse.Content);
            var book = books.FirstOrDefault(b => b["title"]?.ToString() == title);

            Assert.That(book, Is.Not.Null, $"Book with title {title} does not exist");

            var bookId = book["_id"].ToString();

            //Create update request
            var updateRequest = new RestRequest($"/book/{bookId}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            title = title + "Updated Book Title";
            var updatedAuthor = "Updated Author";
            updateRequest.AddJsonBody( new 
            {
                title = title,
                author = updatedAuthor
            });

            //Act
            var updateResponse = client.Execute(updateRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                   "The response does not have the correct status code");

                Assert.That(updateResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var updatedBook = JObject.Parse(updateResponse.Content);

                Assert.That(updatedBook["title"]?.ToString(), Is.EqualTo(title),
                    "Updated title is not as expected");

                Assert.That(updatedBook["author"]?.ToString(), Is.EqualTo(updatedAuthor),
                    "Updated title is not as expected");
            });
        }

        [Test, Order(5)]
        public void Test_DeleteBook()
        {
            //Arrange
            //Get by title
            var getRequest = new RestRequest("/book", Method.Get);

            //Act
            var getResponse = client.Execute(getRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(getResponse.StatusCode, Is.EqualTo
                    (HttpStatusCode.OK), "Response code is not correct");

                Assert.That(getResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");
            });

            var books = JArray.Parse(getResponse.Content);
            var book = books.FirstOrDefault(b => b["title"]?.ToString() == title);

            Assert.That(book, Is.Not.Null, $"Book with title {title} does not exist");

            var bookId = book["_id"].ToString();

            //Create delete request
            var deleteRequest = new RestRequest($"/book/{bookId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            //Act
            var deleteResponse = client.Execute(deleteRequest);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code is not as expected");

                //Get request by id
                var verifyRequest = new RestRequest($"/book/{bookId}", Method.Get);

                var verifyResponse = client.Execute(verifyRequest);

                Assert.That(verifyResponse.Content, Is.EqualTo("null"));
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}

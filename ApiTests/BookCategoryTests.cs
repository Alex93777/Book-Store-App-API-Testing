using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace ApiTests
{
    [TestFixture]
    public class BookCategoryTests : IDisposable
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

        [Test]
        public void Test_BookCategoryLifecycle()
        {
            // Step 1: Create a new book category
            title = $"categoryName_{random.Next(999, 9999)}";
            var createRequest = new RestRequest("/category", Method.Post);
            createRequest.AddHeader("Authorization", $"Bearer {token}");
            createRequest.AddJsonBody(new { title });

            var createResponse = client.Execute(createRequest);

            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Status code is not as expected");

            var createdCategory = JObject.Parse(createResponse.Content);

            var categoryId = createdCategory["_id"]?.ToString();

            Assert.That(categoryId, Is.Not.Null.Or.Empty,
                "Category id is not as expected");

            // Step 2: Retrieve all book categories and verify the newly created category is present
            var getAllCategoriesRequest = new RestRequest("/category", Method.Get);

            var getAllResponse =client.Execute(getAllCategoriesRequest);

            Assert.Multiple(() =>
            {
                Assert.That(getAllResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code is not as expected");

                Assert.That(getAllResponse.Content, Is.Not.Null.Or.Empty,
                    "Response content is not as expected");

                var categories = JArray.Parse(getAllResponse.Content);

                Assert.That(categories?.Type, Is.EqualTo(JTokenType.Array),
                    "Response type is not as expected");

                Assert.That(categories.Count(), Is.GreaterThan(0),
                    "Categories count is less than 1");

                //Find the category by its ID
                var createCategoryObject = categories.FirstOrDefault(c => c["_id"].ToString() == categoryId);

                Assert.That(createCategoryObject, Is.Not.Null);

            });

            // Step 3: Update the category title
            var updateCategoryTitle = new RestRequest($"/category/{categoryId}", Method.Put);
            updateCategoryTitle.AddHeader("Authorization", $"Bearer {token}");
            title = title + "_updated";
            updateCategoryTitle.AddJsonBody(new { title });

            var updateCategoryResponse = client.Execute(updateCategoryTitle);

            Assert.That(updateCategoryResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Step 4: Verify that the category details have been updated
            var getUpdatedCategory = new RestRequest($"/category/{categoryId}", Method.Get);

            var verifyResponse = client.Execute(getUpdatedCategory);

            Assert.That(verifyResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(verifyResponse.Content, Is.Not.Null.Or.Empty);

            var response = JObject.Parse(verifyResponse.Content);

            Assert.That(response["title"]?.ToString(), Is.EqualTo(title));

            // Step 5: Delete the category and validate it's no longer accessible
            var deleteRequest = new RestRequest($"/category/{categoryId}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest);

            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Step 6: Verify that the deleted category cannot be found
            var verifyDeleteRequst = new RestRequest($"/category/{categoryId}", Method.Get);

            var verifyDeleteResponse = client.Execute(verifyDeleteRequst);

            Assert.That(verifyDeleteResponse.Content, Is.EqualTo("null"));
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}

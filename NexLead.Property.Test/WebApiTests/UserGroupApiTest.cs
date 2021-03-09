using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpPoster.Infrastructure.Utilities;
using Xunit;

namespace NexLead.Property.Test.WebApiTests
{
	public class UserGroupApiTest
	{
		private HttpPost poster = new HttpPost(GlobalVariables.HOST);
		private Random random = new Random();


		private List<Dictionary<string, dynamic>> GetUsersByPhoneNumber(HttpPost poster, string phoneNumber)
		{
			var uri = "api/backend/getusers";
			var data = new List<string>()
			{
				phoneNumber
			};

			var result = poster.Post(uri, data).GetAwaiter().GetResult();

			var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			List<Dictionary<string, dynamic>> users = JsonSerializer.Deserialize<List<Dictionary<string, dynamic>>>(content);

			return users;
		}

		private Dictionary<string, dynamic> InitUserData(string phoneNumber)
		{
			return new Dictionary<string, dynamic>()
			{
				{"Salutation", "Mr"},
				{"FullName", $"0 Test {phoneNumber}"},
				{"Gender", "Male"},
				{"NRIC", phoneNumber},
				{"Nationality", "Malaysian"},
				{"MobileNo", phoneNumber},
				{"Email", $"{phoneNumber}@Test.com"},
				{"Address", "sample address"},
				{"Country", "Malaysia"},
				{"State", "Kedah"},
				{"Password", "Welcome123"},
				{"ConfirmPassword", "Welcome123"},
				{"AccountType", "Admin,Sales"},
				{"UserGroups", new string[]{"Sunsuria" } }
			};
		}

		[Fact]
		public async void AddUser_UserInfo_Success()
		{
			var phoneNumber = $"0{random.Next()}0";
			var data = InitUserData(phoneNumber);

			await AddUser(poster, phoneNumber, data);
		}

		private async Task<bool> AddUser(HttpPost poster, string phoneNumber, Dictionary<string, dynamic> data)
		{
			var uri = "api/backend/adduser";
			var adminNSalesType = "2";
			
			try
			{
				var resultAddUser = await poster.Post(uri, data);
				var users = GetUsersByPhoneNumber(poster, phoneNumber);

				var userType = users[0]["AccountType"] + "";

				Assert.True(resultAddUser.IsSuccessStatusCode);
				Assert.Equal(adminNSalesType, userType);

				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				throw;
			}
		}

		[Fact]
		public async void UpdateUser_UserInfo_Success()
		{
			var uri = "api/backend/updateuser";
			var phoneNumber = $"0{random.Next()}0";
			var data = InitUserData(phoneNumber);
			await AddUser(poster, phoneNumber, data);
			var users = GetUsersByPhoneNumber(poster, phoneNumber);
			var oldUser = users[0];

			var newPhoneNumber = $"0{random.Next()}1";
			var newData = InitUserData(newPhoneNumber);
			var userId = oldUser["Id"] + "";
			newData.Add("Id", userId);
			newData["MobileNo"] = data["MobileNo"];

			//Act
			var result = await poster.Post(uri, newData);
			var usersUpdated = GetUsersByPhoneNumber(poster, phoneNumber);

			var content = await result.Content.ReadAsStringAsync();
			var newUser = usersUpdated[0];
			Assert.Equal(newUser["MobileNo"] + "", oldUser["MobileNo"] + "");
			Assert.NotEqual(newUser["Email"] + "", oldUser["Email"] + "");
			Assert.NotEqual(newUser["FullName"] + "", oldUser["FullName"] + "");
		}

	}
}

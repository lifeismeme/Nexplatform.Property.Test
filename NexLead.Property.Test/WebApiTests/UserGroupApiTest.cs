using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HttpPoster.Infrastructure.Utilities;
using NexLead.Property.Services.APIModels;
using Xunit;

namespace NexLead.Property.Test.WebApiTests
{
	public class UserGroupApiTest
	{
		private HttpPost poster = new HttpPost(GlobalVariables.HOST);
		private static Random random = new Random();
		private string phoneNumber = $"8{random.Next()}8";

		private List<UserGroupApiInfo.User> GetUsersByPhoneNumber(HttpPost poster, string phoneNumber)
		{
			var uri = "api/backend/users";
			var data = new List<string>()
			{
				phoneNumber
			};

			var result = poster.Post(uri, data).GetAwaiter().GetResult();

			var content = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			List<UserGroupApiInfo.User> users = JsonSerializer.Deserialize<List<UserGroupApiInfo.User>>(content);

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
				{"Address", $"sample address, date: {DateTime.Now}"},
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
			var data = InitUserData(phoneNumber);

			await AddUser(poster, phoneNumber, data);
		}

		private async Task<bool> AddUser(HttpPost poster, string phoneNumber, Dictionary<string, dynamic> data)
		{
			var uri = "api/backend/users/add";
			var adminNSalesType = "2";

			try
			{
				var resultAddUser = await poster.Post(uri, data);
				var users = GetUsersByPhoneNumber(poster, phoneNumber);

				var userType = users[0].AccountType;

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
			var uri = "api/backend/users/update";
			var data = InitUserData(phoneNumber);
			await AddUser(poster, phoneNumber, data);
			var users = GetUsersByPhoneNumber(poster, phoneNumber);
			var oldUser = users[0];

			//Arrange
			var newPhoneNumber = $"9{random.Next()}8";
			var newData = InitUserData(newPhoneNumber);
			var userId = oldUser.Id;
			newData.Add("Id", userId);
			newData["MobileNo"] = data["MobileNo"];

			//Act
			var result = await poster.Post(uri, newData);
			var usersUpdated = GetUsersByPhoneNumber(poster, phoneNumber);

			var content = await result.Content.ReadAsStringAsync();
			var newUser = usersUpdated[0];
			Assert.Equal(newUser.MobileNo, oldUser.MobileNo);
			Assert.NotEqual(newUser.Email + "", oldUser.Email);
			Assert.NotEqual(newUser.FullName, oldUser.FullName);
		}

		[Fact]
		public async void DeleteUser_UserId_AccountTypeIsZero()
		{
			var uri = "api/backend/users/delete";
			var data = InitUserData(phoneNumber);
			await AddUser(poster, phoneNumber, data);
			var users = GetUsersByPhoneNumber(poster, phoneNumber);
			var oldUser = users[0];
			var userId = oldUser.Id;

			var result = await poster.Post(uri, oldUser);
			var usersUpdated = GetUsersByPhoneNumber(poster, phoneNumber);

			var deletedUser = usersUpdated[0];
			Assert.NotEqual("", userId);
			Assert.True(result.IsSuccessStatusCode);
			Assert.Equal(userId, deletedUser.Id);
			Assert.Equal("0", deletedUser.AccountType);
		}

		private UserGroupApiInfo.UserGroup InitUserGroupData()
		{
			return new UserGroupApiInfo.UserGroup()
			{
				UserGroupName = $"Test Group - {random.Next()}",
				Description = "description here hello test",
				ProjectNames = new string[] { "Sunsuria Forum Corporate Office" },
				RoleIds = new string[] { "5bd215fb-74e3-11eb-8cda-02da6901545c" },
				UserIds = new string[] { "06529711-db74-47ed-b91d-febc33ffd765" }
			};
		}

		private UserGroupApiInfo.UserGroup GetUserGroupByName(string userGroupName)
		{
			var uri = "api/backend/usergroups";
			var data = new string[] { userGroupName };

			var result = poster.Post(uri, data).GetAwaiter().GetResult();
			var json = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			var userGroups = JsonSerializer.Deserialize<List<UserGroupApiInfo.UserGroup>>(json);
			Assert.True(userGroups.Count <= 1);

			return userGroups.Count == 0 ? null : userGroups[0];
		}

		[Fact]
		public async void AddUserGroup_Info_Success()
		{
			var data = InitUserGroupData();

			AddUserGroup(data);
		}

		private UserGroupApiInfo.UserGroup AddUserGroup(UserGroupApiInfo.UserGroup data)
		{
			var uri = "api/backend/usergroups/add";

			var result = poster.Post(uri, data).GetAwaiter().GetResult();
			var userGroup = GetUserGroupByName(data.UserGroupName);

			Assert.NotNull(userGroup);
			Assert.Equal(data.UserGroupName, userGroup.UserGroupName);
			Assert.Equal(data.RoleIds, userGroup.RoleIds);
			Assert.Equal(data.UserIds, userGroup.UserIds);
			Assert.Equal(data.ProjectNames, userGroup.ProjectNames);
			Assert.Equal(data.Description, userGroup.Description);

			return userGroup;
		}

		[Fact]
		public async void UpdateUserGroup_Info_Success()
		{
			var data = InitUserGroupData();
			var oldGroup = AddUserGroup(data);
			var groupName = oldGroup.UserGroupName;

			//Arrange
			var uri = "api/backend/usergroups/update";
			var newData = InitUserGroupData();
			newData.UserGroupName = groupName;
			newData.Description = $"updated description {random.Next()}";

			var resultUpdate = await poster.Post(uri, newData);
			var newGroup = GetUserGroupByName(groupName);

			Assert.Equal(groupName, newGroup.UserGroupName);
			Assert.NotEqual(oldGroup.Description, newGroup.Description);
			//projectIds, userIds, roleIds Not tested yet...
		}

		[Fact]
		public async void DeleteUserGroup_Id_GroupAndUsersDeleted()
		{
			var data = InitUserGroupData();
			var oldGroup = AddUserGroup(data);
			var groupName = oldGroup.UserGroupName;

			//Arrange
			var uri = "api/backend/usergroups/delete";
			var deletedData = InitUserGroupData();
			deletedData.UserGroupName = groupName;
			deletedData.Description = $"test delete this {DateTime.Now}";

			var result = await poster.Post(uri, deletedData);
			var deletedGroup = GetUserGroupByName(groupName);

			Assert.Null(deletedGroup);
		}
	}
}

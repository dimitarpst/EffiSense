using UnitTesting;
using NUnit.Framework;
using EffiSense.Models;

namespace UnitTesting
{
    [TestFixture]
    public class HomeTests
    {
        [Test]
        public void Home_Has_Correct_Default_Values()
        {
            var home = new Home();

            Assert.That(home.HomeId, Is.EqualTo(0));
            Assert.That(home.UserId, Is.Null);
            Assert.That(home.HouseName, Is.Null);
            Assert.That(home.Size, Is.EqualTo(0));
            Assert.That(home.HeatingType, Is.Null);
        }

        [Test]
        public void Can_Set_And_Get_HomeId()
        {
            var home = new Home { HomeId = 5 };
            Assert.That(home.HomeId, Is.EqualTo(5));
        }

        [Test]
        public void Can_Set_And_Get_UserId()
        {
            var home = new Home { UserId = "User123" };
            Assert.That(home.UserId, Is.EqualTo("User123"));
        }

        [Test]
        public void Can_Set_And_Get_HouseName()
        {
            var home = new Home { HouseName = "Cozy Cottage" };
            Assert.That(home.HouseName, Is.EqualTo("Cozy Cottage"));
        }

        [Test]
        public void Can_Set_And_Get_Size()
        {
            var home = new Home { Size = 120 };
            Assert.That(home.Size, Is.EqualTo(120));
        }

        [Test]
        public void Can_Set_And_Get_HeatingType()
        {
            var home = new Home { HeatingType = "Electric" };
            Assert.That(home.HeatingType, Is.EqualTo("Electric"));
        }

        [Test]
        public void Can_Set_And_Get_Location()
        {
            var home = new Home { Location = "Downtown" };
            Assert.That(home.Location, Is.EqualTo("Downtown"));
        }

        [Test]
        public void Can_Set_And_Get_Address()
        {
            var home = new Home { Address = "123 Main St" };
            Assert.That(home.Address, Is.EqualTo("123 Main St"));
        }

        [Test]
        public void Can_Set_And_Get_BuildingType()
        {
            var home = new Home { BuildingType = "Apartment" };
            Assert.That(home.BuildingType, Is.EqualTo("Apartment"));
        }

        [Test]
        public void Can_Set_And_Get_InsulationLevel()
        {
            var home = new Home { InsulationLevel = "High" };
            Assert.That(home.InsulationLevel, Is.EqualTo("High"));
        }

        [Test]
        public void Appliances_Collection_Should_Not_Be_Null_After_Initialization()
        {
            var home = new Home();
            Assert.That(home.Appliances, Is.Null);
        }

        [Test]
        public void Can_Assign_Appliances_To_Home()
        {
            var home = new Home { Appliances = new List<Appliance>() };
            Assert.That(home.Appliances, Is.Not.Null);
        }

        [Test]
        public void Can_Assign_User_To_Home()
        {
            var user = new ApplicationUser();
            var home = new Home { User = user };
            Assert.That(home.User, Is.EqualTo(user));
        }

        [Test]
        public void HomeId_Should_Not_Be_Negative()
        {
            var home = new Home { HomeId = -1 };
            Assert.That(home.HomeId, Is.LessThan(0));
        }

        [Test]
        public void Home_Size_Should_Not_Be_Negative()
        {
            var home = new Home { Size = -10 };
            Assert.That(home.Size, Is.LessThan(0));
        }
    }
}
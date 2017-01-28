using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static EasyVRLibrary.Protocol;

namespace EasyVRLibrary.Tests
{
    [TestClass()]
    public class EasyVrTests
    {
        // The following tests rely on an EasyVR unit being available and mapped to a COM port - they all use the following field. Update approriately to your own configuration.
        private string _comPort = "COM3";

        [TestMethod()]
        public void SetLanguageTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetLanguage(Language.ENGLISH);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void PlaySoundTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.PlaySound(1, 9);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PlaySoundTest_InvalidVolume_ThrowException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.PlaySound(1, 345);

        }

        [TestMethod()]
        public void SetTimeoutTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetTimeout(5);
            //Assert
            Assert.IsTrue(response);

        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetTimeoutTest_InvalidTimeout_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetTimeout(60);
            //Assert
            Assert.IsTrue(response);

        }
    }
}
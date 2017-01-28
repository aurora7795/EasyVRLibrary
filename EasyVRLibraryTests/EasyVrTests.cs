using EasyVRLibrary;
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

        [TestMethod()]
        public void SetMicDistanceTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetMicDistance(Distance.ARMS_LENGTH);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetKnobTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetKnob(Knob.LOOSE);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetTrailingSilenceTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetTrailingSilence(TrailingSilence.TRAILING_300MS);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetLevelTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetLevel(Level.HARD);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetCommandLatencyTest()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetCommandLatency(CommandLatency.MODE_NORMAL);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetDelayTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetDelay(20);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetDelayTest_Rounding10_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetDelay(23);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetDelayTest_Rounding100_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetDelay(93);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void SetDelayTest_Rounding1000_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.SetDelay(223);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void SetDelayTest_OutsideBounds_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.SetDelay(223);
        }

        [TestMethod()]
        public void ChangeBaudrateTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.ChangeBaudrate(Baudrate.B9600);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void AddCommandTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.AddCommand(3, 12);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void AddCommandTest_GroupOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.AddCommand(@group: 17, index: 12);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AddCommandTest_IndexOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.AddCommand(@group: 17, index: 45);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        public void RemoveCommandTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.RemoveCommand(@group: 3, index: 12);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void RemoveCommandTest_GroupOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.RemoveCommand(@group: 17, index: 12);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void RemoveCommandTest_IndexOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.RemoveCommand(@group: 2, index: 45);
        }

        [TestMethod()]
        public void EraseCommandTest_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.EraseCommand(@group: 3, index: 12);
            //Assert
            Assert.IsTrue(response);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void EraseCommandTest_GroupOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.EraseCommand(@group: 17, index: 12);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void EraseCommandTest_IndexOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.EraseCommand(@group: 2, index: 45);
        }

        [TestMethod()]
        public void GetCommandCount_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.GetCommandCount(@group: 3);
            //Assert
            Assert.IsTrue(response >= 0);
        }

        [TestMethod()]
        [ExpectedException(exceptionType: typeof(ArgumentOutOfRangeException))]
        public void GetCommandCount_GroupOutOfRange_ThrowsException()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            tempVr.GetCommandCount(@group: 17);
        }

        [TestMethod()]
        public void GetGrammarsCount_Success()
        {
            //Arrange
            var tempVr = new EasyVr(_comPort);
            //Act
            var response = tempVr.GetGrammarsCount();
            //Assert
            Assert.IsTrue(response >= 0);
        }

        
    }
}
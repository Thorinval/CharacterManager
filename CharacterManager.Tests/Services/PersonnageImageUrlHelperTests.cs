using System;
using Xunit;
using CharacterManager.Server.Services;

namespace CharacterManager.Tests.Services
{
    public class PersonnageImageUrlHelperTests
    {
        [Theory]
        [InlineData("alexa", "Alexa")]
        [InlineData("O-Rinn", "ORinn")]
        [InlineData("zoe et chloe", "ZoeEtChloe")]
        [InlineData("Jean-Paul", "JeanPaul")]
        [InlineData(" ", "")]
        public void NormalizePersonnageName_Returns_PascalCase(string input, string expected)
        {
            var result = PersonnageImageUrlHelper.NormalizePersonnageName(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetImageDetailUrl_Produces_Expected_Path()
        {
            var url = PersonnageImageUrlHelper.GetImageDetailUrl("Alexa");
            Assert.Contains("/api/v1/resources/personnages/", url);
            Assert.EndsWith("/Alexa/alexa.png", url);
        }

        [Fact]
        public void GetImageHeaderUrl_Produces_Expected_Path()
        {
            var url = PersonnageImageUrlHelper.GetImageHeaderUrl("Alexa");
            Assert.Contains("/api/v1/resources/personnages/", url);
            Assert.EndsWith("/Alexa/alexa_header.png", url);
        }

        [Fact]
        public void GetImageSmallPortraitUrl_Produces_Expected_Path()
        {
            var url = PersonnageImageUrlHelper.GetImageSmallPortraitUrl("Alexa");
            Assert.Contains("/api/v1/resources/personnages/", url);
            Assert.EndsWith("/Alexa/alexa_small_portrait.png", url);
        }

        [Fact]
        public void GetImageSmallSelectUrl_Produces_Expected_Path()
        {
            var url = PersonnageImageUrlHelper.GetImageSmallSelectUrl("Alexa");
            Assert.Contains("/api/v1/resources/personnages/", url);
            Assert.EndsWith("/Alexa/alexa_small_select.png", url);
        }

        [Theory]
        [InlineData("Alexa", "", ".png", "/images/personnages/alexa.png")]
        [InlineData("Alexa", "_header", ".png", "/images/personnages/alexa_header.png")]
        public void GetLegacyImageUrl_Produces_Expected_Path(string name, string suffix, string ext, string expectedEnd)
        {
            var url = PersonnageImageUrlHelper.GetLegacyImageUrl(name, suffix, ext);
            Assert.EndsWith(expectedEnd, url);
        }
    }
}

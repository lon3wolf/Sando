using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Sando.DependencyInjection;
using Sando.Recommender;

namespace Sando.IntegrationTests.Search
{
    [TestFixture]
    public class PreSearchRecommendationsTest : AutomaticallyIndexingTestClass
    {
        [Test]
        public void GenerateRecommendationsTest_Play()
        {
            var recommender = ServiceLocator.Resolve<QueryRecommender>();
            var recsForPlay = recommender.GenerateRecommendations("play");
            Assert.IsTrue(recsForPlay.Length > 0, "Did not find any recommendations when I should have");
            Assert.IsTrue(recsForPlay[0].Query.Equals("Player"), "Didn't find the correct first result, found: " + recsForPlay[0].Query);
        }

        [Test]
        public void GenerateRecommendationsTest_Game()
        {
            var recommender = ServiceLocator.Resolve<QueryRecommender>();
            var recsForGame = recommender.GenerateRecommendations("game");
            Assert.IsTrue(recsForGame.Length > 0, "Did not find any recommendations when I should have");
            Assert.IsTrue(recsForGame[0].Query.Equals("GameEngine"), "Didn't find the correct first result, found: " + recsForGame[0].Query);
        }

        public override string GetIndexDirName()
        {
            return "PreSearchRecommendationsTest";
        }

        public override string GetFilesDirectory()
        {
            return "..\\..\\TestInputs\\tictactoe";
        }

        public override TimeSpan? GetTimeToCommit()
        {
            return TimeSpan.FromSeconds(3);
        }
    }
}

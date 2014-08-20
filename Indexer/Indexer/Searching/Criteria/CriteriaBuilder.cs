using System;
using Sando.Core.QueryRefomers;
using Sando.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using Sando.DependencyInjection;
using Configuration.OptionsPages;

namespace Sando.Indexer.Searching.Criteria
{

    public class CriteriaBuilderFactory
    {

        public static CriteriaBuilder GetBuilder()
        {
            return new CriteriaBuilder();
        }

    }

    public class CriteriaBuilder
    {
        SimpleSearchCriteria _searchCriteria;

        private void Initialze(SimpleSearchCriteria searchCriteria)
        {
            if (_searchCriteria == null)
            {
                _searchCriteria = searchCriteria ?? new SimpleSearchCriteria();
            }
        }

        public SimpleSearchCriteria GetCriteria(string searchString, SimpleSearchCriteria searchCriteria = null)
        {
            Initialze(searchCriteria);
            var sandoOptions = ServiceLocator.Resolve<ISandoOptionsProvider>().GetSandoOptions();
            var description = new SandoQueryParser().Parse(searchString);

            AddFromDescription(description);
            _searchCriteria.NumberOfSearchResultsReturned = sandoOptions.NumberOfSearchResultsReturned;
            
            SearchCriteriaReformer.ReformSearchCriteria(_searchCriteria);
            return _searchCriteria;
        }

        private void AddFromDescription(SandoQueryDescription description)
        {
            if (description.AccessLevels.Count > 0)
            {
                _searchCriteria.AccessLevels.Clear();
                _searchCriteria.AddAccessLevels(description.AccessLevels);
            }

            _searchCriteria.FileExtensions.UnionWith(description.FileExtensions);
            _searchCriteria.SearchTerms.UnionWith(description.LiteralSearchTerms);
            _searchCriteria.Locations.UnionWith(description.Locations);

            if (description.ProgramElementTypes.Count > 0)
            {
                _searchCriteria.ProgramElementTypes.Clear();
                _searchCriteria.AddProgramElementTypes(description.ProgramElementTypes);
            }

            _searchCriteria.SearchTerms.UnionWith(description.SearchTerms);

            _searchCriteria.SearchByAccessLevel = _searchCriteria.AccessLevels.Any();
            _searchCriteria.SearchByLocation = _searchCriteria.Locations.Any();
            _searchCriteria.SearchByProgramElementType = _searchCriteria.ProgramElementTypes.Any();
            _searchCriteria.SearchByUsageType = _searchCriteria.UsageTypes.Any();
            _searchCriteria.SearchByFileExtension = _searchCriteria.FileExtensions.Any();
        }
        
    }
}

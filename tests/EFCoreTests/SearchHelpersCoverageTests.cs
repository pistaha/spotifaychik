using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Search.Dtos;
using MusicService.Application.Search.Queries;
using Xunit;

namespace Tests.EFCoreTests
{
    public class SearchHelpersCoverageTests
    {
        [Fact]
        public void SearchQueryHandler_CalculateRelevance_ShouldCoverBranches()
        {
            var dbContext = new Mock<IMusicServiceDbContext>();
            var handler = new SearchQueryHandler(dbContext.Object, NullLogger<SearchQueryHandler>.Instance);
            var method = typeof(SearchQueryHandler).GetMethod("CalculateRelevance", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();

            var starts = (double)method!.Invoke(handler, new object[] { "rockstar", "rock", (List<string>?)null })!;
            var contains = (double)method!.Invoke(handler, new object[] { "mega rock song", "rock", (List<string>?)null })!;
            var genreMatch = (double)method!.Invoke(handler, new object[] { "nomatch", "rock", new List<string> { "Rock" } })!;
            var wordMatch = (double)method!.Invoke(handler, new object[] { "foo bar", "barista", (List<string>?)null })!;
            var none = (double)method!.Invoke(handler, new object[] { "nomatch", "xyz", (List<string>?)null })!;

            starts.Should().Be(1.0);
            contains.Should().Be(0.5);
            genreMatch.Should().Be(0.3);
            wordMatch.Should().Be(0.2);
            none.Should().Be(0.0);
        }

        [Fact]
        public void AdvancedSearchQueryHandler_CalculateAdvancedRelevance_ShouldCoverBranches()
        {
            var dbContext = new Mock<IMusicServiceDbContext>();
            var handler = new AdvancedSearchQueryHandler(dbContext.Object, NullLogger<AdvancedSearchQueryHandler>.Instance);
            var method = typeof(AdvancedSearchQueryHandler).GetMethod("CalculateAdvancedRelevance", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();

            var exact = (double)method!.Invoke(handler, new object[] { "rock", "rock", (List<string>?)null })!;
            var starts = (double)method!.Invoke(handler, new object[] { "rockstar", "rock", (List<string>?)null })!;
            var contains = (double)method!.Invoke(handler, new object[] { "mega rock song", "rock", (List<string>?)null })!;
            var withGenres = (double)method!.Invoke(handler, new object[] { "nomatch", "rock", new List<string> { "Rock", "Pop" } })!;

            exact.Should().BeGreaterThan(1.9);
            starts.Should().BeGreaterThan(1.4);
            contains.Should().BeGreaterThan(0.9);
            withGenres.Should().BeGreaterThan(0.1);
        }

        [Fact]
        public void AdvancedSearchQueryHandler_ApplyFilters_ShouldFilterByDateAndCategory()
        {
            var dbContext = new Mock<IMusicServiceDbContext>();
            var handler = new AdvancedSearchQueryHandler(dbContext.Object, NullLogger<AdvancedSearchQueryHandler>.Instance);
            var method = typeof(AdvancedSearchQueryHandler).GetMethod("ApplyFilters", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();

            var now = DateTime.UtcNow;
            var results = new List<AdvancedSearchResultDto>
            {
                new() { Type = "artist", CreatedAt = now.AddDays(-2), Title = "A" },
                new() { Type = "album", CreatedAt = now.AddDays(-1), Title = "B" },
                new() { Type = "track", CreatedAt = now, Title = "C" }
            };

            var request = new AdvancedPaginationRequest
            {
                CreatedFrom = now.AddDays(-1),
                CreatedTo = now.AddHours(-12),
                Categories = new[] { "album", "track" }
            };

            var args = new object[] { results, request };
            method!.Invoke(handler, args);
            var filtered = (List<AdvancedSearchResultDto>)args[0];

            filtered.Should().OnlyContain(r => r.Type == "album" || r.Type == "track");
            filtered.Should().OnlyContain(r => r.CreatedAt >= request.CreatedFrom);
        }

        [Fact]
        public void AdvancedSearchQueryHandler_ApplySorting_ShouldSortByTitleAndRelevance()
        {
            var dbContext = new Mock<IMusicServiceDbContext>();
            var handler = new AdvancedSearchQueryHandler(dbContext.Object, NullLogger<AdvancedSearchQueryHandler>.Instance);
            var method = typeof(AdvancedSearchQueryHandler).GetMethod("ApplySorting", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();

            var results = new List<AdvancedSearchResultDto>
            {
                new() { Title = "B", RelevanceScore = 0.2, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { Title = "A", RelevanceScore = 0.9, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new() { Title = "C", RelevanceScore = 0.5, CreatedAt = DateTime.UtcNow }
            };

            var titleRequest = new AdvancedPaginationRequest { SortBy = "title", SortOrder = "asc" };
            var titleArgs = new object[] { results.ToList(), titleRequest };
            method!.Invoke(handler, titleArgs);
            ((List<AdvancedSearchResultDto>)titleArgs[0]).Select(r => r.Title).Should().ContainInOrder("A", "B", "C");

            var titleDescRequest = new AdvancedPaginationRequest { SortBy = "title", SortOrder = "desc" };
            var titleDescArgs = new object[] { results.ToList(), titleDescRequest };
            method.Invoke(handler, titleDescArgs);
            ((List<AdvancedSearchResultDto>)titleDescArgs[0]).Select(r => r.Title).Should().ContainInOrder("C", "B", "A");

            var relevanceRequest = new AdvancedPaginationRequest { SortBy = "relevance" };
            var relevanceArgs = new object[] { results.ToList(), relevanceRequest };
            method.Invoke(handler, relevanceArgs);
            ((List<AdvancedSearchResultDto>)relevanceArgs[0]).First().RelevanceScore.Should().Be(0.9);

            var defaultRequest = new AdvancedPaginationRequest { SortOrder = "asc" };
            var defaultArgs = new object[] { results.ToList(), defaultRequest };
            method.Invoke(handler, defaultArgs);
            ((List<AdvancedSearchResultDto>)defaultArgs[0]).First().CreatedAt.Should().Be(results.Min(r => r.CreatedAt));
        }
    }
}

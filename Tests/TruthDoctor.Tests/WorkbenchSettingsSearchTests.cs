using System;
using TruthDoctor.Controllers.Workbench;
using Xunit;

namespace TruthDoctor.Tests;

public sealed class WorkbenchSettingsSearchTests
{
    [Fact]
    public void SearchKindsRepresentSettingsRoadmapDomains()
    {
        var kinds =
            Enum.GetValues<WorkbenchSettingsSearchDocumentKind>();

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.PersonalPreference,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Administration,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Provider,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Policy,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Approval,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Integration,
            kinds);

        Assert.Contains(
            WorkbenchSettingsSearchDocumentKind.Governance,
            kinds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSearchMatchesDocument(
        string? searchText)
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                searchText));
    }

    [Theory]
    [InlineData("compact-density")]
    [InlineData("dark")]
    [InlineData("aws")]
    [InlineData("aws-production-readonly")]
    [InlineData("production-protection")]
    [InlineData("cloud-engineer")]
    [InlineData("production-restart")]
    [InlineData("sunday-maintenance")]
    [InlineData("operations-alerts")]
    [InlineData("servicenow")]
    [InlineData("change-governance")]
    public void AuthoritativeSettingsFieldsAreSearchable(
        string searchText)
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                searchText));
    }

    [Fact]
    public void SearchIsTrimmedAndCaseInsensitive()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                "  AWS-PRODUCTION-READONLY  "));
    }

    [Fact]
    public void PartialFieldMatchIsSupported()
    {
        var document =
            CreateDocument();

        Assert.True(
            WorkbenchSettingsSearch.Matches(
                document,
                "production-re"));
    }

    [Fact]
    public void DisplayTextIsNotAnImplicitSearchField()
    {
        var document =
            CreateDocument();

        document.DisplayText =
            "unique-display-only-text";

        Assert.False(
            WorkbenchSettingsSearch.Matches(
                document,
                "unique-display-only-text"));
    }

    [Fact]
    public void SearchKindIsNotAnImplicitSearchField()
    {
        var document =
            CreateDocument();

        document.Kind =
            WorkbenchSettingsSearchDocumentKind.Integration;

        Assert.False(
            WorkbenchSettingsSearch.Matches(
                document,
                "Integration"));
    }

    [Fact]
    public void SearchDocumentDoesNotExposeRawSecretProperties()
    {
        var propertyNames =
            Array.ConvertAll(
                typeof(WorkbenchSettingsSearchDocument)
                    .GetProperties(),
                property => property.Name);

        Assert.DoesNotContain(
            "Password",
            propertyNames);

        Assert.DoesNotContain(
            "Secret",
            propertyNames);

        Assert.DoesNotContain(
            "SecretKey",
            propertyNames);

        Assert.DoesNotContain(
            "Token",
            propertyNames);

        Assert.DoesNotContain(
            "PrivateKey",
            propertyNames);

        Assert.DoesNotContain(
            "CredentialValue",
            propertyNames);
    }

    [Fact]
    public void UnrelatedSearchDoesNotMatch()
    {
        var document =
            CreateDocument();

        Assert.False(
            WorkbenchSettingsSearch.Matches(
                document,
                "does-not-exist"));
    }

    [Fact]
    public void NullAndEmptyFieldsAreSafe()
    {
        var document =
            new WorkbenchSettingsSearchDocument
            {
                Kind =
                    WorkbenchSettingsSearchDocumentKind.Administration,

                DisplayText =
                    "Sparse settings document",

                PersonalPreference = null,
                Appearance = "",
                Provider = null,
                CredentialReference = "",
                Policy = null,
                Role = "",
                ApprovalRule = null,
                MaintenanceWindow = "",
                Notification = null,
                Integration = "",
                GovernanceSetting = null
            };

        Assert.False(
            WorkbenchSettingsSearch.Matches(
                document,
                "anything"));
    }

    [Fact]
    public void NullDocumentIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchSettingsSearch.Matches(
                null!,
                "anything"));
    }

    [Fact]
    public void SearchReturnsMatchingDocuments()
    {
        var matching =
            CreateDocument();

        var unrelated =
            CreateDocument();

        unrelated.PersonalPreference =
            "comfortable-density";

        unrelated.Appearance =
            "light";

        unrelated.Provider =
            "azure";

        unrelated.CredentialReference =
            "azure-development-reader";

        unrelated.Policy =
            "development-policy";

        unrelated.Role =
            "viewer";

        unrelated.ApprovalRule =
            "development-change";

        unrelated.MaintenanceWindow =
            "saturday-maintenance";

        unrelated.Notification =
            "development-alerts";

        unrelated.Integration =
            "jira";

        unrelated.GovernanceSetting =
            "development-governance";

        var results =
            WorkbenchSettingsSearch.Search(
                new[]
                {
                    matching,
                    unrelated
                },
                "aws-production-readonly");

        var result =
            Assert.Single(results);

        Assert.Same(
            matching,
            result);
    }

    [Fact]
    public void SearchPreservesInputOrder()
    {
        var first =
            CreateDocument();

        first.Policy =
            "production-protection-primary";

        var second =
            CreateDocument();

        second.Policy =
            "production-protection-secondary";

        var results =
            WorkbenchSettingsSearch.Search(
                new[]
                {
                    first,
                    second
                },
                "production-protection");

        Assert.Equal(
            2,
            results.Count);

        Assert.Same(
            first,
            results[0]);

        Assert.Same(
            second,
            results[1]);
    }

    [Fact]
    public void BlankSearchReturnsOneEntryPerInputDocument()
    {
        var first =
            CreateDocument();

        var second =
            CreateDocument();

        var results =
            WorkbenchSettingsSearch.Search(
                new[]
                {
                    first,
                    second
                },
                "   ");

        Assert.Equal(
            2,
            results.Count);

        Assert.Same(
            first,
            results[0]);

        Assert.Same(
            second,
            results[1]);
    }

    [Fact]
    public void SearchReturnsEmptyWhenNothingMatches()
    {
        var results =
            WorkbenchSettingsSearch.Search(
                new[]
                {
                    CreateDocument()
                },
                "does-not-exist");

        Assert.Empty(results);
    }

    [Fact]
    public void NullDocumentCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WorkbenchSettingsSearch.Search(
                null!,
                "anything"));
    }

    private static WorkbenchSettingsSearchDocument
        CreateDocument()
    {
        return new WorkbenchSettingsSearchDocument
        {
            Kind =
                WorkbenchSettingsSearchDocumentKind.Administration,

            DisplayText =
                "Production administration settings",

            PersonalPreference =
                "compact-density",

            Appearance =
                "dark",

            Provider =
                "aws",

            CredentialReference =
                "aws-production-readonly",

            Policy =
                "production-protection",

            Role =
                "cloud-engineer",

            ApprovalRule =
                "production-restart",

            MaintenanceWindow =
                "sunday-maintenance",

            Notification =
                "operations-alerts",

            Integration =
                "servicenow",

            GovernanceSetting =
                "change-governance"
        };
    }
}

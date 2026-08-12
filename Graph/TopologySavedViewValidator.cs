using System;
using System.Collections.Generic;
using System.Linq;

namespace TruthDoctor.Graph;

public sealed class TopologySavedViewValidator
{
    public IReadOnlyList<string> Validate(
        TopologySavedView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        var errors =
            new List<string>();

        if (view.SchemaVersion !=
            TopologySavedView.CurrentSchemaVersion)
        {
            errors.Add(
                "SchemaVersion is not supported.");
        }

        if (string.IsNullOrWhiteSpace(view.Id))
        {
            errors.Add("Id is required.");
        }

        if (string.IsNullOrWhiteSpace(view.Name))
        {
            errors.Add("Name is required.");
        }

        if (view.CreatedAtUtc == default)
        {
            errors.Add("CreatedAtUtc is required.");
        }

        if (view.UpdatedAtUtc == default)
        {
            errors.Add("UpdatedAtUtc is required.");
        }

        if (view.UpdatedAtUtc < view.CreatedAtUtc)
        {
            errors.Add(
                "UpdatedAtUtc cannot precede CreatedAtUtc.");
        }

        if (view.Depth is < 1 or > 3)
        {
            errors.Add(
                "Depth must be between 1 and 3.");
        }

        if (!Enum.IsDefined(view.LayoutMode))
        {
            errors.Add(
                "LayoutMode is invalid.");
        }

        if (string.IsNullOrWhiteSpace(
                view.SelectedDomainId))
        {
            errors.Add(
                "SelectedDomainId is required.");
        }

        if (view.RelationshipFilters is null)
        {
            errors.Add(
                "RelationshipFilters is required.");
        }

        if (!double.IsFinite(view.Zoom) ||
            view.Zoom is < 0.35 or > 2.00)
        {
            errors.Add(
                "Zoom must be between 0.35 and 2.00.");
        }

        if (!double.IsFinite(
                view.ScrollOffset.X) ||
            !double.IsFinite(
                view.ScrollOffset.Y) ||
            view.ScrollOffset.X < 0 ||
            view.ScrollOffset.Y < 0)
        {
            errors.Add(
                "ScrollOffset must contain finite, non-negative values.");
        }

        if (view.CollapsedDomainIds is null)
        {
            errors.Add(
                "CollapsedDomainIds is required.");
        }
        else
        {
            if (view.CollapsedDomainIds.Any(
                    string.IsNullOrWhiteSpace))
            {
                errors.Add(
                    "CollapsedDomainIds cannot contain blank values.");
            }

            var distinctCount =
                view.CollapsedDomainIds
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .Count();

            if (distinctCount !=
                view.CollapsedDomainIds.Length)
            {
                errors.Add(
                    "CollapsedDomainIds cannot contain duplicates.");
            }
        }

        return errors;
    }

    public void EnsureValid(
        TopologySavedView view)
    {
        var errors =
            Validate(view);

        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Saved topology view is invalid: " +
            string.Join("; ", errors),
            nameof(view));
    }
}

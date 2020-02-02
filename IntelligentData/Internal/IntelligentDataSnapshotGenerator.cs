
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace IntelligentData.Internal
{
    /// <summary>
    /// Snapshot generator that simply removes the IntelligentData annotations from migration snapshots.
    /// </summary>
    public sealed class IntelligentDataSnapshotGenerator : CSharpSnapshotGenerator
    {
        /// <summary>
        /// Creates the snapshot generator.
        /// </summary>
        /// <param name="dependencies"></param>
        public IntelligentDataSnapshotGenerator(CSharpSnapshotGeneratorDependencies dependencies) 
            : base(dependencies)
        {
            
        }

        /// <inheritdoc />
        protected override void IgnoreAnnotations(IList<IAnnotation> annotations, params string[] annotationNames)
        {
            base.IgnoreAnnotations(annotations, annotationNames);

            foreach (var annotation in annotations
                                       .Where(x => x.Name.StartsWith("IntelligentData:", StringComparison.OrdinalIgnoreCase))
                                       .ToList())
            {
                annotations.Remove(annotation);
            }
        }
    }
}

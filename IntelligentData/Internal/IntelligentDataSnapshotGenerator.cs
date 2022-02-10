using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace IntelligentData.Internal
{
    /// <summary>
    /// Snapshot generator that simply removes the IntelligentData annotations from migration snapshots.
    /// </summary>
    public sealed class IntelligentDataSnapshotGenerator : CSharpSnapshotGenerator
    {
        private class AnnotationCodeGeneratorWrapper : IAnnotationCodeGenerator
        {
            private readonly IAnnotationCodeGenerator _generator;

            public AnnotationCodeGeneratorWrapper(IAnnotationCodeGenerator generator)
            {
                _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            }

            public IEnumerable<IAnnotation> FilterIgnoredAnnotations(IEnumerable<IAnnotation> annotations)
            {
                var ret = _generator.FilterIgnoredAnnotations(annotations).ToList();
                ret.RemoveAll(x => x.Name.StartsWith("IntelligentData:", StringComparison.OrdinalIgnoreCase));
                return ret;
            }
        }

        /// <summary>
        /// Creates the snapshot generator.
        /// </summary>
        /// <param name="dependencies"></param>
        public IntelligentDataSnapshotGenerator(CSharpSnapshotGeneratorDependencies dependencies)
            : base(
#pragma warning disable EF1001
                new CSharpSnapshotGeneratorDependencies(
#pragma warning restore EF1001
                    dependencies.CSharpHelper,
                    dependencies.RelationalTypeMappingSource,
                    new AnnotationCodeGeneratorWrapper(dependencies.AnnotationCodeGenerator)
                )
            )
        {
        }
    }
}

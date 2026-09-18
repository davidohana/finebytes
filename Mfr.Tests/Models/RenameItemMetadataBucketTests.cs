namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for parameterized disk metadata bucket state on <see cref="RenameItem"/>.
    /// </summary>
    public sealed class RenameItemMetadataBucketTests
    {
        /// <summary>
        /// Verifies mark / error / clear round-trip through the single-flag bucket API.
        /// </summary>
        [Theory]
        [InlineData(RenameListMetadataRequirement.TagLib)]
        [InlineData(RenameListMetadataRequirement.ImageProperties)]
        [InlineData(RenameListMetadataRequirement.Pdf)]
        public void Metadata_bucket_state_round_trips(RenameListMetadataRequirement bucket)
        {
            var item = new RenameItem(new FileMeta(0, 0, @"C:\tmp", "row", "bin", fileSize: 1));

            Assert.False(item.WasMetadataLoadAttempted(bucket));
            Assert.Null(item.GetMetadataLoadError(bucket));

            item.MarkMetadataLoadAttempted(bucket);
            Assert.True(item.WasMetadataLoadAttempted(bucket));

            var ex = new InvalidOperationException("boom");
            item.SetMetadataLoadError(bucket, ex);
            Assert.Same(ex, item.GetMetadataLoadError(bucket));

            item.ClearMetadataLoadState(bucket);
            Assert.False(item.WasMetadataLoadAttempted(bucket));
            Assert.Null(item.GetMetadataLoadError(bucket));
        }

        /// <summary>
        /// Verifies combined or unknown requirement flags are rejected.
        /// </summary>
        [Theory]
        [InlineData(RenameListMetadataRequirement.None)]
        [InlineData(RenameListMetadataRequirement.TagLib | RenameListMetadataRequirement.Pdf)]
        [InlineData((RenameListMetadataRequirement)8)]
        public void RequireSingle_rejects_non_bucket_flags(RenameListMetadataRequirement bucket)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RenameListMetadataBuckets.RequireSingle(bucket));
        }

        /// <summary>
        /// Verifies <see cref="RenameListMetadataBuckets.All"/> lists each single disk bucket once.
        /// </summary>
        [Fact]
        public void All_lists_taglib_image_and_pdf()
        {
            Assert.Equal(
                [
                    RenameListMetadataRequirement.TagLib,
                    RenameListMetadataRequirement.ImageProperties,
                    RenameListMetadataRequirement.Pdf,
                ],
                RenameListMetadataBuckets.All
            );
        }
    }
}

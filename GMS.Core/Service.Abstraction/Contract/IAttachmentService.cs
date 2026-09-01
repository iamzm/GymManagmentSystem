namespace Services.Abstraction.Contract {
    /// <summary>
    /// Stores And Removes Uploaded Files. Declared In Terms Of A Plain <see cref="Stream"/> So The
    /// Core Stays Free Of Any Web-Framework Types; The Presentation Layer Adapts Its Upload Type To It.
    /// </summary>
    public interface IAttachmentService {
        /// <returns>The Stored File Name, Or Null When The Upload Was Rejected.</returns>
        Task<string?> UploadAsync(Stream content, string originalFileName, string folderName);
        void Delete(string? fileName, string folderName);
        bool IsAllowed(string originalFileName, long lengthInBytes);
    }
}

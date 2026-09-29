namespace YellowCola.Catalog.Domain.Categories
{
    public sealed class Category
    {
        private Category()
        {
        }

        public Category(
            Guid id,
            string name,
            string slug)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Category id cannot be empty.",
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Category name is required.",
                    nameof(name));
            }

            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new ArgumentException(
                    "Category slug is required.",
                    nameof(slug));
            }

            Id = id;
            Name = name.Trim();
            Slug = slug.Trim().ToLowerInvariant();
            IsActive = true;
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Slug { get; private set; } = string.Empty;

        public bool IsActive { get; private set; }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Category name is required.",
                    nameof(name));
            }

            Name = name.Trim();
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
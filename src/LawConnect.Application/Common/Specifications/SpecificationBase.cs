using System.Linq.Expressions;

namespace LawConnect.Application.Common.Specifications;

/// <summary>
/// Base helper for building specifications without duplicating include/order/paging logic.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class SpecificationBase<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationBase{T}"/> class.
    /// </summary>
    /// <param name="criteria">Optional filter expression.</param>
    protected SpecificationBase(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria;
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public int Take { get; private set; }

    /// <inheritdoc />
    public int Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; } = true;

    /// <summary>
    /// Replaces the filter criteria.
    /// </summary>
    /// <param name="criteria">Filter expression.</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    /// <summary>
    /// Adds a strongly typed include.
    /// </summary>
    /// <param name="includeExpression">Include expression.</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression) =>
        _includes.Add(includeExpression);

    /// <summary>
    /// Adds a string include path.
    /// </summary>
    /// <param name="includeString">Include path.</param>
    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);

    /// <summary>
    /// Applies ascending ordering.
    /// </summary>
    /// <param name="orderByExpression">Order expression.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) =>
        OrderBy = orderByExpression;

    /// <summary>
    /// Applies descending ordering.
    /// </summary>
    /// <param name="orderByDescendingExpression">Order expression.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) =>
        OrderByDescending = orderByDescendingExpression;

    /// <summary>
    /// Enables paging.
    /// </summary>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Enables change tracking for the query.
    /// </summary>
    protected void EnableTracking() => AsNoTracking = false;
}

using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

public interface INntpClientRfc6048
{
    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.2">LIST COUNTS</a>
    /// command returns a list of valid newsgroups carried by
    /// the news server along with associated information, the "counts list",
    /// and is similar to LIST ACTIVE.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A groups response containing a list of valid newsgroups.</returns>
    Task<NntpGroupsResponse> ListCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.2">LIST COUNTS</a>
    /// command returns a list of valid newsgroups carried by
    /// the news server along with associated information, the "counts list",
    /// and is similar to LIST ACTIVE.
    /// </summary>
    /// <param name="wildmat">The wildmat to use for filtering the group names.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A groups response object containing a list of valid newsgroups.</returns>
    Task<NntpGroupsResponse> ListCountsAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.3">LIST DISTRIBUTIONS</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.4">ad 1</a>)
    /// command returns the distributions
    /// file which is maintained by some news transport systems
    /// to contain information about valid values for the Distribution: line
    /// in a news article header and about what the values mean.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the distributions list.</returns>
    Task<NntpMultiLineResponse> ListDistributionsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.4">LIST MODERATORS</a>
    /// command return the "moderators list" which is maintained by some NNTP servers to make
    /// clients aware of how the news server will generate a submission
    /// e-mail address when an article is locally posted to a moderated
    /// newsgroup.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the moderators list.</returns>
    Task<NntpMultiLineResponse> ListModeratorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.5">LIST MOTD</a>
    /// command contains a "message of the day" relevant to the news server.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response object containing the message of the day.</returns>
    Task<NntpMultiLineResponse> ListMotdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-2.6">LIST SUBSCRIPTIONS</a>
    /// (<a href="https://tools.ietf.org/html/rfc2980#section-2.1.8">ad 1</a>)
    /// command is used to get a default subscription list for new users
    /// of this server.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A multi-line response containing a list of recommended subscriptions.</returns>
    Task<NntpMultiLineResponse> ListSubscriptionsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-3">LIST ACTIVE</a>
    /// (<a href="https://tools.ietf.org/html/rfc3977#section-7.6.3">ad 1</a>,
    /// <a href="https://tools.ietf.org/html/rfc2980#section-2.1.2">ad 2</a>)
    /// command returns a list of valid newsgroups and associated
    /// information.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A groups response object containing a list of valid newsgroups and associated information.</returns>
    Task<NntpGroupsResponse> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The <a href="https://tools.ietf.org/html/rfc6048#section-3">LIST ACTIVE</a>
    /// (<a href="https://tools.ietf.org/html/rfc3977#section-7.6.3">ad 1</a>,
    /// <a href="https://tools.ietf.org/html/rfc2980#section-2.1.2">ad 2</a>)
    /// command returns a list of valid newsgroups and associated
    /// information.
    /// </summary>
    /// <param name="wildmat">The wildmat to use for filtering the group names.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A groups response object containing a list of valid newsgroups and associated information.</returns>
    Task<NntpGroupsResponse> ListActiveAsync(
        string wildmat,
        CancellationToken cancellationToken = default
    );
}

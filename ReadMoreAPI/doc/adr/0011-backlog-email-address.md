# 9. Backlog Email Address

Date: 2017-10-22

## Status

Accepted

## Context

One of the sources for links that are eventually saved to Pocket are email newsletters. However, a user often does not want to put all
articles from a newsletter in to Pocket. So, newsletters waiting to be reviewed can clutter an email inbox.

## Decision

We will give each user an email address they can send emails containing articles to. The user will then be able to review these articles
and optionally save them to Pocket.

## Consequences

Handling inbound email will most likely require the use of a transactional email service, such as MailGun, that can POST to an API endpoint
when an email is received.

The email address for a user will be used to determine what list to add articles to, meaning that it must be unique.

We can't use a protected account ID in the email address as this is likely to be too long for users to want to use.

Using an unprotected account ID in the email address for a user will expose the plaintext and cipher, which could make it easier for an
attacker to determine the private key material.

To prevent both of the above issues, a new UUID will need to be generated for use in the email address for a user.

Emails will consist of a variety of formats, which may cause issues in identifying links.

Some links will not be relevant and will need to be removed (i.e. unsubscribe links).

A webpage will need to be provided to view the list of articles to be reviewed. This could be part of the existing chrome extension.
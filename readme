# Avoiding Cartesian products in database queries

NHibernate allows one to configure eager fetching of related entities.
It can be configured either in the (Fluent NHibernate) class map via `.Not.LazyLoad`
or in the query itself with `.Fetch` or `.FetchMany`.
However related collections are eager loaded via outer joins.
That leads to Cartesian product query results when there are multiple related collections eager loaded.
I.e. for each resulting entity the underlying database query returns the number of rows
calculated as a product of the number of related entities in each related collection being eager fetched.
For example, if for a person we have 10 emails, 10 phone numbers, and 10 post addresses,
the underlying database query will return 1000 rows.
Each row contains all the fields from the queried entity and all the related entities being eager fetched.
The database has to spend CPU time and memory to generate those rows, the rows have to be transmitted over the network,
NHibernate has to spend CPU time to parse those rows. All that combined can significantly reduce performance.
And it's hard to discover because it has non-linear dependency on the related collection sizes.

To avoid Cartesian product query results the related collections should be eager loaded in different queries.
If possible, those queries should be executed in one query batch to avoid multiple request delays.
However, databases have limitation on a number of query batch parameters.
If the query is filtered by a potentially large collection of values,
each value is passed in a separate parameter to each query
so the collection should be split in batches
and each batch of the collection values should be executed in its own database query batch.
That is non-trivial to implement correctly and to cover with tests.
So I decided to build a fluent interface that takes care of the complexity.

##Usage scenarios

1. When there is a fixed number of query parameters which is small enough
so the query batch parameter limits are not exceeded even with multiple queries in a query batch

  ```
  // The usual query
  IQueryable<Person> query = session.Query<Person>()
      .Where(person => person.FistName == "John")
      .Where(person => person.LastName == "Doe")
      .FetchMany(person => person.Emails)
      .FetchMany(person => person.PhoneNumbers)
      .FetchMany(person => person.PostAddresses);

  List<Person> result = query
      .AvoidingCartesianProducts()
      .ToList();
  ```

2. When the query is filtered by a potentially large collection of values

  ```
  // The usual query without the filtering by the potentially large collection of values
  IQueryable<Person> query = session.Query<Person>()
      .Where(person => person.FirstName == "John")
      .FetchMany(person => person.Emails)
      .FetchMany(person => person.PhoneNumbers)
      .FetchMany(person => person.PostAddresses);

  // We don't include .Where(person => lastNames.Contains(person.LastName))

  List<Person> result = query
      .AvoidingCartesianProducts()
      .InBatches(session, getMaxSqlQueryParameterCount)
      .FilteredBy(lastNames)
      .FilteredOn(person => person.LastName)
      .ToUnorderedDistinctList();
  ```

  Note: Since the query is split in batches, it cannot sort the resulting values in the database and cannot return duplicate entities.
  
  The `getMaxSqlQueryParameterCount` delegate is responsible for determining the maximum query parameter count
  allowed by the database being used.

##Limitations

Only eager loading configured in the query itself via `Fetch` and `FetchMany` is supported.
The class maps should not configure any eager loading.

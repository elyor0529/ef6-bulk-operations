﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanneryd.BulkOperations.EF6.Model;
using Tanneryd.BulkOperations.EF6.Tests.DM.Blog;
using Tanneryd.BulkOperations.EF6.Tests.DM.Numbers;
using Tanneryd.BulkOperations.EF6.Tests.EF;

namespace Tanneryd.BulkOperations.EF6.Tests
{
    [TestClass]
    public class BulkInsertOneToManyTests : BulkOperationTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeNumberContext();
            InitializeBlogContext();
        }

        [TestCleanup]
        public void CleanUp()
        {
            CleanupNumberContext();
            CleanupBlogContext();
        }

        /// <summary>
        /// Test that one-to-many hierarchies can be bulk inserted where
        /// we have a guid as primary, and foreign, key. Also, in this test
        /// we insert the entity on the "one" side and expect that all
        /// attached child entities are also inserted properly.
        /// </summary>
        [TestMethod]
        public void OneToManyWithGuidPrimaryKeyInsertingTheTopEntity()
        {
            using (var db = new BlogContext())
            {
                var blog = new Blog { Name = "My Blog" };
                var firstPost = new Post
                {
                    Text = "My first blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "first" } }
                };
                var secondPost = new Post
                {
                    Text = "My second blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "second" } }
                };
                blog.BlogPosts.Add(firstPost);
                blog.BlogPosts.Add(secondPost);
                var req = new BulkInsertRequest<Blog>
                {
                    Entities = new[] { blog }.ToList(),
                    AllowNotNullSelfReferences = false,
                    SortUsingClusteredIndex = true,
                    Recursive = true
                };
                var response = db.BulkInsertAll(req);
                var posts = db.Posts
                    .Include(p=>p.Blog)
                    .ToArray();
                Assert.AreEqual(2, posts.Count());
                Assert.AreEqual(posts[1].Blog, posts[0].Blog);
            }
        }


        /// <summary>
        /// Test that one-to-many hierarchies can be bulk inserted where
        /// we have a guid as primary, and foreign, key. Also, in this test
        /// we insert the entities on the "many" side and expect that the
        /// attached parent entity is also inserted properly.
        /// </summary>
        [TestMethod]
        public void OneToManyWithGuidPrimaryKeyInsertingTheChildEntities()
        {
            using (var db = new BlogContext())
            {
                var blog = new Blog { Name = "My Blog" };
                var firstPost = new Post
                {
                    Blog = blog,
                    Text = "My first blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "first" } }
                };
                var secondPost = new Post
                {
                    Blog = blog,
                    Text = "My second blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "second" } }
                };
                var req = new BulkInsertRequest<Post>
                {
                    Entities = new[] { firstPost, secondPost }.ToList(),
                    AllowNotNullSelfReferences = false,
                    SortUsingClusteredIndex = true,
                    Recursive = true
                };
                var response = db.BulkInsertAll(req);
                var posts = db.Posts
                    .Include(p => p.Blog)
                    .ToArray();
                Assert.AreEqual(2, posts.Count());
                Assert.AreEqual(posts[1].Blog, posts[0].Blog);
            }
        }
       
        /// <summary>
        /// We use parity to test the one-to-many relationship. Each number
        /// has a foreign key relation to one of the two parity entries.
        /// </summary>
        [TestMethod]
        public void OneToManyWhereTheOneAlreadyExists()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                Parity even = new Parity { Name = "Even", UpdatedAt = now, UpdatedBy = "Måns" };
                Parity odd = new Parity { Name = "Odd", UpdatedAt = now, UpdatedBy = "Måns" };
                db.BulkInsertAll(new[] { even, odd });

                var numbers = GenerateNumbers(1, 100, even, odd, now).ToArray();
                db.BulkInsertAll(numbers);

                Assert.AreEqual(100, db.Numbers.Count());

                var dbNumbers = db.Numbers.Include(n => n.Parity).ToArray();
                foreach (var number in dbNumbers.Where(n => n.Value % 2 == 0))
                {
                    Assert.AreEqual("Even", number.Parity.Name);
                    Assert.AreEqual(now.ToString("yyyyMMddHHmmss"), number.UpdatedAt.ToString("yyyyMMddHHmmss"));
                }

                foreach (var number in dbNumbers.Where(n => n.Value % 2 != 0))
                {
                    Assert.AreEqual("Odd", number.Parity.Name);
                    Assert.AreEqual(now.ToString("yyyyMMddHHmmss"), number.UpdatedAt.ToString("yyyyMMddHHmmss"));
                }
            }
        }

        [TestMethod]
        public void OneToManyWhereAllIsNew()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                var numbers = GenerateNumbers(1, 100, now).ToArray();
                var request = new BulkInsertRequest<Number>
                {
                    Entities = numbers,
                    Recursive = true,
                };
                db.BulkInsertAll(request);

                Assert.AreEqual(100, db.Numbers.Count());

                var dbNumbers = db.Numbers.Include(n => n.Parity).ToArray();
                foreach (var number in dbNumbers.Where(n => n.Value % 2 == 0))
                {
                    Assert.AreEqual("Even", number.Parity.Name);
                    Assert.AreEqual(now.ToString("yyyyMMddHHmmss"), number.UpdatedAt.ToString("yyyyMMddHHmmss"));
                }

                foreach (var number in dbNumbers.Where(n => n.Value % 2 != 0))
                {
                    Assert.AreEqual("Odd", number.Parity.Name);
                    Assert.AreEqual(now.ToString("yyyyMMddHHmmss"), number.UpdatedAt.ToString("yyyyMMddHHmmss"));
                }
            }
        }
    }
}
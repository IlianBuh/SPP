using System.Collections.Generic;
using MySubjectProject.Models;

namespace MySubjectProject.Repositories
{
    public interface IDriverRepository
    {
        void Add(Driver driver);
        Driver GetById(int id);
        List<Driver> GetAll();
        void Update(Driver driver);
        void Clear();
    }
}


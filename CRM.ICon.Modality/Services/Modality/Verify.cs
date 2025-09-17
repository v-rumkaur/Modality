namespace CRM.ICon.Modality.Services.Modality
{
    public class Verify
    {
        /// <summary>
        /// Verify an argument is not null and throws exception if it is
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="item">Argument value</param>
        /// <param name="name">Argument name</param>
        /// <returns>Argument value that can be assigned to another variable</returns>
        public static T NotNull<T>(T item, string name) where T : class
        {
            if (item == null)
            {
                throw new ArgumentNullException(name);
            }

            return item;
        }
    }
}


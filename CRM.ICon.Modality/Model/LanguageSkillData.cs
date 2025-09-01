namespace CRM.ICon.Modality.Model
{
    public class LanguageSkillData
    {
        /// <summary>
        /// Skill name
        /// </summary>
        private LanguageSkill skillName;

        /// <summary>
        /// The default value of skill data
        /// </summary>
        public static ICollection<LanguageSkillData> Default
        {
            get
            {
                return new List<LanguageSkillData>
                            {
                                new LanguageSkillData() { SkillName = LanguageSkill.ENG },
                            };
            }
        }

        /// <summary>
        /// Skill name
        /// </summary>
        public LanguageSkill SkillName
        {
            get
            {
                return this.skillName;
            }

            set
            {
                this.skillName = value;
            }
        }

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>String representation of a skill</returns>
        public override string ToString()
        {
            return ((int)this.SkillName).ToString();
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>True if other object is SkillData and SkillName matches this.</returns>
        public override bool Equals(object obj)
        {
            return obj != null && obj as LanguageSkillData != null && ((LanguageSkillData)obj).SkillName == this.SkillName;
        }

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns>Hash code for a skill</returns>
        public override int GetHashCode()
        {
            return this.SkillName.GetHashCode();
        }
    }
}

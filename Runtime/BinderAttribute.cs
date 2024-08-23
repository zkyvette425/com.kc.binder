using System;

namespace KC
{
    public class BinderAttribute : Attribute
    {
        public string Annotation { get; private set; }
        
        public string Path { get; private set; }

        public BinderAttribute(string path , string annotation = null)
        {
            Annotation = annotation;

            Path = string.IsNullOrEmpty(path)
                ? "Generate/Binder"
                : path;
        }
    }
}


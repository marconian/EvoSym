using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utilities.Model
{

    public class BlockTemplate
    {
        public BlockTemplate(string name, Vector3 position, Vector3 rotation, BodyTemplate template, float mutationChance = .9f)
        {
            Name = name;
            Position = position;
            Rotation = rotation;
            Template = template;
            MutationChance = mutationChance;
        }
        public string Name { get; }
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
        private BodyTemplate Template { get; }
        public float MutationChance { get; set; }


        private BlockTemplateSides _sides;
        public BlockTemplateSides Sides
        {
            get
            {
                if (_sides == null)
                    _sides = new BlockTemplateSides(Position, Template.Template.Keys);
                else _sides.Update(Position, Template.Template.Keys);

                return _sides;
            }
        }

    }
}

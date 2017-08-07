﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheWitnessPuzzleGenerator
{
    public class Sector
    {
        public List<Block> Blocks { get; set; }

        public int TotalBlocks => Blocks.Count;
        public Puzzle Panel { get; }

        public Sector(List<Block> blocks)
        {
            Blocks = blocks;
            foreach (var block in Blocks)
                block.CurrentSector = this;

            Panel = Blocks.FirstOrDefault()?.ParentPanel;
        }

        public override string ToString() => string.Join(" ", Blocks);

        public List<Error> CheckSectorErrors(IEnumerable<Node> solutionNodes, IEnumerable<Edge> solutionEdges)
        {
            List<Error> errorsList = new List<Error>();
            errorsList.AddRange(CheckSectorSelfCheckableBlockErrors());
            errorsList.AddRange(CheckSectorNodeErrors(solutionNodes));
            errorsList.AddRange(CheckSectorEdgeErrors(solutionEdges));

            errorsList.AddRange(CheckTetrisErrors());

            return errorsList;
        }

        private IEnumerable<Error> CheckTetrisErrors()
        {
            List<Error> errorsList = new List<Error>();

            // Create board where sector blocks are 0 and other blocks are 1
            // Complete board should have only Ones and no Zeroes
            int[,] baseBoard = new int[Panel.Width, Panel.Height];
            for (int j = 0; j < Panel.Height; j++)
                for (int i = 0; i < Panel.Width; i++)
                    if (Blocks.Contains(Panel.grid[i, j]))
                        baseBoard[i, j] = 0;
                    else
                        baseBoard[i, j] = 1;

            // All variations of board, created by different placement of subtractive shapes
            IEnumerable<int[,]> allBoards;

            // Retrieve all Tetris rules from sector blocks
            var tetrisRules = Blocks.Where(x => x.Rule is TetrisRule).Select(x => x.Rule as TetrisRule);

            // If net sum of all tetris blocks is not equal to total blocks of sector, then it's an error outright
            if (tetrisRules.Sum(x => x.TotalBlocks) != TotalBlocks)
            {
                foreach (var tetromino in tetrisRules)
                    errorsList.Add(new Error(tetromino.OwnerBlock, null));
                return errorsList;
            }

            var rotatableTetrominos = tetrisRules.Where(x => x is TetrisRotatableRule).Select(x => x as TetrisRotatableRule);
            var stationaryTetrominos = tetrisRules.Except(rotatableTetrominos);

            // If there are no subtracting tetrominos, then we are lucky to deal with only one version of board
            if (!tetrisRules.Any(x => x.IsSubtractive))
                allBoards = new List<int[,]> { baseBoard };
            // Otherwise we have to create variations of base board, where all subtractive tetrominos are placed in every possible combination
            else
            {
                // Extract subtractive tetrominos
                var rotatableSubtractive = rotatableTetrominos.Where(x => x.IsSubtractive);
                var stationarySubtractive = stationaryTetrominos.Where(x => x.IsSubtractive);

                // Remove subtractive ones from regular ones
                rotatableTetrominos = rotatableTetrominos.Except(rotatableSubtractive);
                stationaryTetrominos = stationaryTetrominos.Except(stationarySubtractive);

                // Get all combinations of rotations of subtractive shapes
                List<List<TetrisRule>> allSubtractiveCombinations = GetAllTetrominoCombinations(stationarySubtractive, rotatableSubtractive);

                // Now for every combination of rotations create all combinations of positions of every shape on board
                // Create a board version from each combination and compile them into all boards collection
                allBoards = GetAllBoards();

                IEnumerable<int[,]> GetAllBoards()
                {
                    // For every rotations combination
                    foreach (var rotatCombination in allSubtractiveCombinations)
                    {
                        // Get all possible positions on board for every shape
                        // And try to place shapes according to these positions for every positions variant
                        foreach (var permut in GetPermutationsWithRepetitions(rotatCombination.Count, Panel.Width * Panel.Height))
                        {
                            int[,] boardCopy = baseBoard.Clone() as int[,];
                            bool failedCombination = false;

                            for (int i = 0; i < rotatCombination.Count; i++)
                            {
                                int y = permut[i] / Panel.Width;
                                int x = permut[i] - y * Panel.Width;

                                // If shape can't be placed at specified position, this variant is invalid => skip
                                if (!ApplyShapeToBoard(boardCopy, rotatCombination[i].Shape, (x, y), true))
                                {
                                    failedCombination = true;
                                    break;
                                }
                            }

                            if (failedCombination)
                                continue;

                            // If all shapes in variant are placed on board, then return this board as one of board variants
                            yield return boardCopy;
                        }
                    }
                }
            }

            // Get all combinations of rotations of tetrominos
            List<List<TetrisRule>> allTetrominoCombinations = GetAllTetrominoCombinations(stationaryTetrominos, rotatableTetrominos);

            // Now, for each board variation and each tetromino rotations combination we should generate all permutations 
            // of tetrominos order and try to place tetrominos from given rotation variant onto board variant in given order

            // List of all possible orders. One order is a list of indices of shapes
            IEnumerable<IEnumerable<int>> tetrominosOrders = GetPermutations(allTetrominoCombinations.First().Count);

            // If we will find such combination of board/rotation/order that fits into board and makes it complete with 1's
            // Then we will set this flag and abort further calculations
            bool success = false;

            // For each board
            foreach (int[,] board in allBoards)
            {
                // For each rotations variant
                foreach (List<TetrisRule> rotationComb in allTetrominoCombinations)
                {
                    // For each order
                    foreach (IEnumerable<int> order in tetrominosOrders)
                    {
                        int[,] boardCopy = board.Clone() as int[,];
                        bool allShapesPlaced = true;

                        // For each shape in order list
                        foreach (int shapeIndex in order)
                        {
                            // Flag indicates that shape was successfully placed on board
                            // If shape was not placed on board, then there's no need to try other shapes in this order
                            bool shapePlaced = false;

                            // Try to place a shape on board starting from top-left most position
                            for (int pos = 0; pos < Panel.Width * Panel.Height; pos++)
                            {
                                int y = pos / Panel.Width;
                                int x = pos - y * Panel.Width;

                                // Once current shape successfully placed on board
                                // break and go to the next shape in current order
                                if (true == ApplyShapeToBoard(boardCopy, rotationComb[shapeIndex].Shape, (x, y)))
                                {
                                    shapePlaced = true;
                                    break;
                                }
                            }

                            // If shape was not placed on board, there's no need to try other shapes in this order
                            if (!shapePlaced)
                            {
                                allShapesPlaced = false;
                                break;
                            }
                        }

                        // If we interrupted previous loop due to not placed shape, then there's no need to check if board is complete
                        if (!allShapesPlaced)
                            continue;

                        // Once all shapes are placed, we should check if board is complete (all blocks are 1)
                        // Let's assume that board is complete
                        success = true;
                        for (int i = 0; i < boardCopy.GetLength(0); i++)
                        {
                            for (int j = 0; j < boardCopy.GetLength(1); j++)
                                // If at least one block is not 1, then board is not complete
                                if (boardCopy[i, j] != 1)
                                {
                                    success = false; // BUSTED
                                    break;
                                }
                            if (!success)
                                break;
                        }

                        // Keep looping untill the success or the end
                        if (success)
                            break;
                    }

                    if (success)
                        break;
                }

                if (success)
                    break;
            }

            // If after all of this loops we couldn't find a right combination for solution
            // Then create errors for each Tetris rule block and finally return
            if (!success)
                foreach (var tetrisRule in tetrisRules)
                    errorsList.Add(new Error(tetrisRule.OwnerBlock, null));
            
            return errorsList;

            // ==============================================
            // ============== Local methods =================
            bool ApplyShapeToBoard(int[,] board, bool[,] shape, (int x, int y) point, bool isSubtractive = false)
            {
                int[,] originalBoard = board.Clone() as int[,];

                if (point.x + shape.GetLength(0) - 1 >= board.GetLength(0) ||
                   point.y + shape.GetLength(1) - 1 >= board.GetLength(1))
                    return false;

                for (int i = 0; i < shape.GetLength(0); i++)
                    for (int j = 0; j < shape.GetLength(1); j++)
                        if (shape[i, j])
                            if (board[point.x + i, point.y + j] < 1 || isSubtractive)
                                board[point.x + i, point.y + j] += isSubtractive ? -1 : 1;
                            // If shape is not subtractive and it's can't be fit onto board (not all blocks have 0 or less)
                            // Then restore board to it's original state and return fail
                            else
                            {
                                for (int w = 0; w < board.GetLength(0); w++)
                                    for (int z = 0; z < board.GetLength(1); z++)
                                        board[w, z] = originalBoard[w, z];
                                return false;
                            }

                return true;
            }

            IEnumerable<List<int>> GetPermutationsWithRepetitions(int places, int options = 4)
            {
                int numericMax = (int) Math.Pow(options, places);

                for (int i = 0; i < numericMax; i++)
                {
                    List<int> li = new List<int>(places);
                    for (int digit = 0; digit < places; digit++)
                    {
                        li.Add((int) (i / Math.Pow(options, digit)) % options);
                    }
                    yield return li;
                }
            }

            // Rotate every rotatable shape and create every possible combination of rotations of each shape
            // Combined with non-rotatable shapes
            List<List<TetrisRule>> GetAllTetrominoCombinations(IEnumerable<TetrisRule> stationary, IEnumerable<TetrisRotatableRule> rotatable)
            {
                // Shapes rotations - list of array of 4 rotations for every rotatable shape
                List<TetrisRotatableRule[]> shapeRotations = new List<TetrisRotatableRule[]>();
                foreach (var shape in rotatable)
                {
                    TetrisRotatableRule[] shapes = new TetrisRotatableRule[4];
                    shapes[0] = shape;
                    for (int i = 1; i < 4; i++)
                        shapes[i] = shapes[i - 1].RotateCW();
                    shapeRotations.Add(shapes);
                }

                List<List<TetrisRule>> allCombinations = new List<List<TetrisRule>>();
                // If there're no rotatable shapes, then there's only one combination of shape rotations
                if (shapeRotations.Count == 0)
                    allCombinations.Add(new List<TetrisRule>(stationary));
                // Otherwise get all combinations of rotations of every rotatable (and add non-rotatables to every combination)
                else
                    foreach (var permut in GetPermutationsWithRepetitions(shapeRotations.Count, 4))
                    {
                        List<TetrisRule> combination = new List<TetrisRule>();
                        for (int i = 0; i < shapeRotations.Count; i++)
                            combination.Add(shapeRotations[i][permut[i]]);
                        combination.AddRange(stationary);

                        allCombinations.Add(combination);
                    }

                return allCombinations;
            }

            IEnumerable<IEnumerable<int>> GetPermutations(int places) => Enumerable.Range(0, places).Permute();
        }

        private List<Error> CheckSectorSelfCheckableBlockErrors()
        {
            List<Error> errorsList = new List<Error>();
            foreach (Block block in Blocks)
            {
                if (block.Rule is ISelfCheckableRule rule)
                {
                    var error = rule.CheckRule();
                    if (error != null)
                        errorsList.Add(error);
                }
            }

            return errorsList;
        }

        private List<Error> CheckSectorNodeErrors(IEnumerable<Node> solutionNodes)
        {
            List<Error> errorsList = new List<Error>();

            var markedNodes = Blocks.SelectMany(x => x.Nodes).Distinct().Where(x => x.State == NodeState.Marked);

            // Each marked node without solution going through it is an error
            foreach (var node in markedNodes.Except(solutionNodes))
                errorsList.Add(new Error(node, null));

            return errorsList;
        }

        private List<Error> CheckSectorEdgeErrors(IEnumerable<Edge> solutionEdges)
        {
            List<Error> errorsList = new List<Error>();

            var markedEdges = Blocks.SelectMany(x => x.Edges).Distinct().Where(x => x.State == EdgeState.Marked);
            
            // Each marked edge without solution going through it is an error
            foreach (var edge in markedEdges.Except(solutionEdges))
                errorsList.Add(new Error(edge, null));

            return errorsList;
        }
    }
}

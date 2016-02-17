﻿namespace SymTensor



module DerivCheck =

    open ArrayNDNS


    /// evaluates the Jacobian of f at x numerically with specified finite difference step
    let numGradEpsilon epsilon f x =
        let y = f x
        let xShp, yShp = ArrayND.shape x, ArrayND.shape y
        let xElems, yElems = ArrayND.nElems x, ArrayND.nElems y
        let xf, yf = x |> ArrayND.reshape [xElems], y |> ArrayND.reshape [yElems]

        let j = ArrayNDHost.zeros [yElems; xElems]
        for xi = 0 to xElems - 1 do
            let xdf = ArrayND.copy xf
            xdf |> ArrayND.set [xi] ((xf |> ArrayND.get [xi]) + epsilon)
            let ydf = xdf |> ArrayND.reshape xShp |> f |> ArrayND.reshape [yElems]
            let d = (ydf - yf) / epsilon       
            j |> ArrayND.view [All; Elem xi] |> ArrayND.copyTo d
        j

    /// evaluates the Jacobian of f at x numerically
    let numGrad f x = numGradEpsilon 1e-5 f x

    //let exprGradDiff evalEnv wrt expr =
    //    let g = ExprForwardDiff.grad wrt expr
    //    let exprFun = (expr |> OpEval.toFun |> OpEval.addArg wrt) |> OpEval.usingEvalEnv evalEnv
    //    let gradFun = (g |> OpEval.toFun |> OpEval.addArg wrt) |> OpEval.usingEvalEnv evalEnv
    //
    //    let value = evalEnv.VarEnv.[Op.extractVar wrt]
    //    let symGradVal = gradFun value
    //    let exprGradVal = numGrad exprFun value
    //    let gradDiff = abs (symGradVal - exprGradVal)
    //    sum gradDiff |> NDArray.value


    let reverseDiffDeviations evalEnv expr =
        let mutable devs = Map.empty
        let rDiffs = Deriv.compute expr
        for wrt, rDiff in rDiffs |> Map.toSeq do
            let exprFun = (expr |> Eval.toFun |> Eval.addArg (Expr.makeVar wrt)) |> Eval.usingEvalEnv evalEnv
            let rDiffFun = (rDiff |> Eval.toFun |> Eval.addArg (Expr.makeVar wrt)) |> Eval.usingEvalEnv evalEnv

            let value = EvalEnv.getVarSpecT wrt evalEnv
            let symGradVal = rDiffFun value
            let exprGradVal = numGrad exprFun value
            let gradDiff = abs (symGradVal - exprGradVal)
            devs <- devs |> Map.add (VarSpec.name wrt) (ArrayND.sum gradDiff |> ArrayND.value)
        devs

    let reverseDiffDeviationsOkay evalEnv expr =
        let maxDeviation = 1e-4
        reverseDiffDeviations evalEnv expr |> Map.iter
            (fun name dev -> if dev > maxDeviation then printfn "deviation wrt %s = %f" name dev)
        reverseDiffDeviations evalEnv expr |> Map.forall (fun _ dev -> dev < maxDeviation) 




    let checkReverseDiff (evalEnv: EvalEnvT) expr = 
        let evalEnv = evalEnv |> EvalEnv.enhance VarEnv.empty (Seq.singleton expr)

        let rec checkSubExpr expr = 
            match expr with
            | Expr.Leaf(_) -> ()
            | Expr.Unary(_, a) -> 
                checkSubExpr a
            | Expr.Binary(_, a, b) -> 
                checkSubExpr a
                checkSubExpr b
            | Expr.Nary(_, es) ->
                es |> List.iter checkSubExpr

            if not (reverseDiffDeviationsOkay evalEnv expr) then
                failwithf "deviation between numeric and symbolic derivative too large in op %A" (UExpr.extractOp expr)

        checkSubExpr expr






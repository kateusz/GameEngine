#!/bin/bash

# Game Engine Benchmark Runner
# Usage: ./run-benchmark.sh [options]

set -e

# Default values
TESTS="all"
DURATION="5"
ENTITIES="1000"
THRESHOLD="10"
OUTPUT="benchmark-results.json"
BASELINE=""
VERBOSE=""
NO_FAIL=""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --save-baseline)
            SAVE_BASELINE=true
            shift
            ;;
        --compare-with-baseline)
            BASELINE="benchmark-baseline.json"
            shift
            ;;
        --tests)
            TESTS="$2"
            shift 2
            ;;
        --duration)
            DURATION="$2"
            shift 2
            ;;
        --entities)
            ENTITIES="$2"
            shift 2
            ;;
        --threshold)
            THRESHOLD="$2"
            shift 2
            ;;
        --output)
            OUTPUT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE="--verbose"
            shift
            ;;
        --no-fail-on-regression)
            NO_FAIL="--no-fail-on-regression"
            shift
            ;;
        --help|-h)
            echo "Game Engine Benchmark Runner"
            echo ""
            echo "Usage: ./run-benchmark.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --save-baseline            Save results as new baseline"
            echo "  --compare-with-baseline    Compare with existing baseline"
            echo "  --tests <tests>            Tests to run (default: all)"
            echo "  --duration <seconds>       Test duration (default: 5)"
            echo "  --entities <count>         Entity count (default: 1000)"
            echo "  --threshold <percent>      Regression threshold (default: 10)"
            echo "  --output <file>            Output file (default: benchmark-results.json)"
            echo "  --verbose                  Enable verbose output"
            echo "  --no-fail-on-regression    Don't exit with error on regression"
            echo "  --help, -h                 Show this help message"
            echo ""
            echo "Examples:"
            echo "  # Run all benchmarks and save as baseline"
            echo "  ./run-benchmark.sh --save-baseline"
            echo ""
            echo "  # Compare with existing baseline"
            echo "  ./run-benchmark.sh --compare-with-baseline"
            echo ""
            echo "  # Run specific test with custom settings"
            echo "  ./run-benchmark.sh --tests Renderer2DStress --duration 10 --entities 5000"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║        Game Engine Benchmark Runner                        ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Build the project
echo "Building benchmark project..."
dotnet build Benchmark/Benchmark.csproj --configuration Release -v quiet

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Build successful${NC}"
echo ""

# Prepare benchmark command
BENCHMARK_CMD="dotnet run --project Benchmark --configuration Release -- --headless"
BENCHMARK_CMD="$BENCHMARK_CMD --tests $TESTS"
BENCHMARK_CMD="$BENCHMARK_CMD --duration $DURATION"
BENCHMARK_CMD="$BENCHMARK_CMD --entities $ENTITIES"
BENCHMARK_CMD="$BENCHMARK_CMD --threshold $THRESHOLD"
BENCHMARK_CMD="$BENCHMARK_CMD --output $OUTPUT"

if [ -n "$VERBOSE" ]; then
    BENCHMARK_CMD="$BENCHMARK_CMD $VERBOSE"
fi

if [ -n "$NO_FAIL" ]; then
    BENCHMARK_CMD="$BENCHMARK_CMD $NO_FAIL"
fi

if [ -n "$BASELINE" ] && [ -f "$BASELINE" ]; then
    BENCHMARK_CMD="$BENCHMARK_CMD --baseline $BASELINE"
    echo "Comparing with baseline: $BASELINE"
fi

# Run benchmark
echo "Running benchmarks..."
echo ""

eval $BENCHMARK_CMD
EXIT_CODE=$?

echo ""

# Handle results
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Benchmark completed successfully${NC}"

    # Save as baseline if requested
    if [ "$SAVE_BASELINE" = true ]; then
        cp "$OUTPUT" benchmark-baseline.json
        echo -e "${GREEN}✓ Saved as baseline: benchmark-baseline.json${NC}"
    fi

    exit 0
elif [ $EXIT_CODE -eq 1 ]; then
    echo -e "${RED}✗ Performance regression detected${NC}"
    exit 1
else
    echo -e "${RED}✗ Benchmark failed with error${NC}"
    exit 2
fi
